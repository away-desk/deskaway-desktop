using System.Reflection;
using System.Text;
using System.Text.Json;
using Json.Schema;

namespace DeskAway.Protocol;

/// <summary>Whether to check the payload of a message addressed to someone else.</summary>
public enum PayloadCheck
{
    /// <summary>Only when the message is addressed to the endpoint checking it. What production uses.</summary>
    Addressed,
    /// <summary>Always. For fixtures and tests.</summary>
    Always,
}

/// <summary>The outcome of checking one frame. Exactly one of <see cref="Message"/> and <see cref="CloseReason"/> is set.</summary>
public sealed record CheckResult<T>
    where T : Envelope
{
    /// <summary>Whether the frame passed every check.</summary>
    public bool Ok => Message is not null;

    /// <summary>The typed message, when it passed.</summary>
    public T? Message { get; init; }

    /// <summary>The first check it failed, when it did not.</summary>
    public CloseReason? CloseReason { get; init; }

    /// <summary>Why it failed, for logs. Never sent to the peer.</summary>
    public string? Detail { get; init; }
}

/// <summary>
/// The C# port of deskaway-protocol's runtime/check-message.mjs. Same order,
/// same close reasons, and it must agree with the JavaScript and Python
/// checkers on every example — Contract.Tests runs all of them.
/// <list type="number">
/// <item>size over 256 KiB → message-too-large (before parsing)</item>
/// <item>not JSON → invalid-json</item>
/// <item>envelopeVersion a number other than 1 → unsupported-envelope-version</item>
/// <item>inbound frame with a relay block → relay-fields-from-sender</item>
/// <item>type not a known message type → unknown-type</item>
/// <item>fails the envelope schema → invalid-envelope</item>
/// <item>fails the payload schema, checked only by the addressee → invalid-payload</item>
/// </list>
/// Build one with <see cref="Create"/> and reuse it: loading the schemas is the expensive part.
/// </summary>
public sealed class MessageChecker
{
    /// <summary>The largest frame accepted: 256 KiB of UTF-8.</summary>
    public const int MaxMessageBytes = 256 * 1024;

    /// <summary>The only envelope version that exists.</summary>
    public const int EnvelopeVersion = 1;

    private const string Base = "https://deskaway.dev/protocol/";
    private const string ResourcePrefix = "contract/";

    private static readonly EvaluationOptions Evaluation = new() { OutputFormat = OutputFormat.Flag };
    private static readonly JsonDocumentOptions Parsing = new() { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow };

    private readonly JsonSchema _inbound;
    private readonly JsonSchema _outbound;
    private readonly Dictionary<string, JsonSchema> _payloads;
    private readonly HashSet<string> _messageTypes;

    private MessageChecker(JsonSchema inbound, JsonSchema outbound, Dictionary<string, JsonSchema> payloads, HashSet<string> messageTypes)
    {
        _inbound = inbound;
        _outbound = outbound;
        _payloads = payloads;
        _messageTypes = messageTypes;
    }

    /// <summary>The message types the embedded contract defines, as they appear on the wire.</summary>
    public IReadOnlyCollection<string> MessageTypes => _messageTypes;

    /// <summary>Loads and compiles every schema embedded from the pinned protocol commit. Never touches the network.</summary>
    public static MessageChecker Create()
    {
        var assembly = typeof(MessageChecker).Assembly;
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            var text = ReadResource(assembly, name);
            if (!string.IsNullOrWhiteSpace(text))
            {
                texts[name[ResourcePrefix.Length..]] = text;
            }
        }

        if (texts.Count == 0)
        {
            throw new InvalidOperationException("No protocol schemas are embedded; the build did not fetch the contract.");
        }

        var registry = new SchemaRegistry { Fetch = static (uri, _) => throw new InvalidOperationException($"Refusing to fetch schema {uri}: every schema must be embedded.") };
        var build = new BuildOptions { SchemaRegistry = registry };

        // Enums first, then the shared envelope definitions, then everything that
        // refers to them, so every $ref resolves against something already built.
        var order = texts.Keys
            .OrderBy(p => p.StartsWith("enums/", StringComparison.Ordinal) ? 0 : p == "schemas/envelope.v1.json" ? 1 : 2)
            .ThenBy(p => p, StringComparer.Ordinal);
        var byPath = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var path in order)
        {
            byPath[path] = JsonSchema.FromText(texts[path], build, new Uri(Base + path));
        }

        using var messageTypeEnum = JsonDocument.Parse(texts["enums/message-type.json"]);
        var messageTypes = messageTypeEnum.RootElement.GetProperty("enum").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);

        var payloads = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var type in messageTypes)
        {
            var matches = byPath.Keys.Where(p => p.StartsWith("schemas/", StringComparison.Ordinal) && p.Count(c => c == '/') == 2 && p.EndsWith($"/{type}.v1.json", StringComparison.Ordinal)).ToList();
            if (matches.Count != 1)
            {
                throw new InvalidOperationException($"Message type \"{type}\" matches {matches.Count} payload schemas, expected 1.");
            }

            payloads[type] = byPath[matches[0]];
        }

        return new MessageChecker(byPath["schemas/envelope-inbound.v1.json"], byPath["schemas/envelope-outbound.v1.json"], payloads, messageTypes);
    }

    /// <summary>A frame from a device, as the relay receives it. A relay block is refused.</summary>
    public CheckResult<InboundEnvelope> CheckInbound(string frame, Endpoint self = Endpoint.Relay, PayloadCheck payload = PayloadCheck.Addressed) =>
        Check<InboundEnvelope>(frame, inbound: true, self, payload);

    /// <summary>A frame from the relay, as a device receives it. A relay block is required.</summary>
    public CheckResult<OutboundEnvelope> CheckOutbound(string frame, Endpoint self, PayloadCheck payload = PayloadCheck.Addressed) =>
        Check<OutboundEnvelope>(frame, inbound: false, self, payload);

    private CheckResult<T> Check<T>(string frame, bool inbound, Endpoint self, PayloadCheck payload)
        where T : Envelope
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Encoding.UTF8.GetByteCount(frame) > MaxMessageBytes)
        {
            return Fail<T>(CloseReason.MessageTooLarge);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(frame, Parsing);
        }
        catch (JsonException e)
        {
            return Fail<T>(CloseReason.InvalidJson, e.Message);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Fail<T>(CloseReason.InvalidEnvelope, "the top level is not an object");
            }

            if (root.TryGetProperty("envelopeVersion", out var version) && version.ValueKind == JsonValueKind.Number && version.GetDouble() != EnvelopeVersion)
            {
                return Fail<T>(CloseReason.UnsupportedEnvelopeVersion);
            }

            if (inbound && root.TryGetProperty("relay", out _))
            {
                return Fail<T>(CloseReason.RelayFieldsFromSender);
            }

            if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String && !_messageTypes.Contains(typeElement.GetString()!))
            {
                return Fail<T>(CloseReason.UnknownType);
            }

            if (!(inbound ? _inbound : _outbound).Evaluate(root, Evaluation).IsValid)
            {
                return Fail<T>(CloseReason.InvalidEnvelope, "the envelope does not match its schema");
            }

            var type = root.GetProperty("type").GetString()!;
            var addressedHere = root.GetProperty("to").GetString() == WireName(self);
            if ((payload == PayloadCheck.Always || addressedHere) && !_payloads[type].Evaluate(root.GetProperty("payload"), Evaluation).IsValid)
            {
                return Fail<T>(CloseReason.InvalidPayload, $"the {type} payload does not match its schema");
            }

            try
            {
                return new CheckResult<T> { Message = root.Deserialize<T>(ProtocolJson.Options) };
            }
            catch (JsonException e)
            {
                // The schema accepted it but the C# types did not: a drift between this
                // project and the schema, which Contract.Tests exists to catch first.
                return Fail<T>(CloseReason.InvalidEnvelope, $"schema-valid but not readable as {typeof(T).Name}: {e.Message}");
            }
        }
    }

    private static CheckResult<T> Fail<T>(CloseReason reason, string? detail = null)
        where T : Envelope => new() { CloseReason = reason, Detail = detail };

    private static string WireName(Endpoint endpoint) => JsonSerializer.Serialize(endpoint, ProtocolJson.Options).Trim('"');

    private static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
