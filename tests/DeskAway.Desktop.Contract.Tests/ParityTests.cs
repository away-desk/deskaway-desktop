using System.Text.Json;
using System.Text.Json.Nodes;
using DeskAway.Protocol;

namespace DeskAway.Desktop.Contract.Tests;

/// <summary>
/// The hand-written C# compared with the schemas it was written from. Examples
/// only exercise the fields they contain; these checks cover every field,
/// including optional ones no example happens to use.
/// </summary>
public sealed class ParityTests
{
    [Theory]
    [InlineData("enums/message-type.json", typeof(MessageType))]
    [InlineData("enums/endpoint.json", typeof(Endpoint))]
    [InlineData("enums/close-reason.json", typeof(CloseReason))]
    public void Enum_has_exactly_the_values_of_its_protocol_enum_file(string file, Type enumType)
    {
        var expected = Contract.Json(file)["enum"]!.AsArray().Select(v => v!.GetValue<string>());
        AssertSameSet(expected, WireNames(enumType), $"{enumType.Name} vs {file}");
    }

    [Fact]
    public void Hello_role_has_exactly_the_values_of_the_schema()
    {
        var expected = Contract.Json("schemas/control/hello.v1.json")["properties"]!["role"]!["enum"]!.AsArray().Select(v => v!.GetValue<string>());
        AssertSameSet(expected, WireNames(typeof(HelloRole)), "HelloRole");
    }

    [Fact]
    public void Every_message_type_has_a_payload_record()
    {
        foreach (var type in Enum.GetValues<MessageType>())
        {
            Assert.True(PayloadTypes.ByMessageType.ContainsKey(type), $"no payload record for {type}");
        }
    }

    public static TheoryData<string, Type> Payloads => new()
    {
        { "schemas/control/hello.v1.json", typeof(HelloPayload) },
        { "schemas/control/heartbeat.v1.json", typeof(HeartbeatPayload) },
        { "schemas/control/goodbye.v1.json", typeof(GoodbyePayload) },
        { "schemas/control/session-claim.v1.json", typeof(SessionClaimPayload) },
        { "schemas/control/session-evicted.v1.json", typeof(SessionEvictedPayload) },
    };

    [Theory]
    [MemberData(nameof(Payloads))]
    public void Payload_record_has_the_schemas_fields_and_required_set(string file, Type record)
    {
        var schema = Contract.Json(file);
        AssertFieldsMatch(schema, record, file);
    }

    [Fact]
    public void Payload_parity_covers_every_message_type()
    {
        var covered = Payloads.Select(row => (Type)row.Data.Item2).ToHashSet();
        foreach (var record in PayloadTypes.ByMessageType.Values)
        {
            Assert.Contains(record, covered);
        }
    }

    [Fact]
    public void Inbound_envelope_has_the_common_fields_and_no_relay_block()
    {
        var common = Contract.Json("schemas/envelope.v1.json")["$defs"]!["common"]!;
        AssertFieldsMatch(common, typeof(InboundEnvelope), "envelope common fields");
        Assert.DoesNotContain("relay", Fields(typeof(InboundEnvelope)).Names);
    }

    [Fact]
    public void Outbound_envelope_has_the_common_fields_plus_a_required_relay_block()
    {
        var common = Contract.Json("schemas/envelope.v1.json")["$defs"]!["common"]!.DeepClone().AsObject();
        common["properties"]!.AsObject()["relay"] = new JsonObject();
        common["required"]!.AsArray().Add("relay");
        AssertFieldsMatch(common, typeof(OutboundEnvelope), "outbound envelope");
    }

    [Fact]
    public void Relay_block_has_the_schemas_fields_and_required_set()
    {
        var relayBlock = Contract.Json("schemas/envelope.v1.json")["$defs"]!["relayBlock"]!;
        AssertFieldsMatch(relayBlock, typeof(RelayBlock), "relay block");
    }

    private static void AssertFieldsMatch(JsonNode schema, Type record, string what)
    {
        var properties = schema["properties"]?.AsObject().Select(p => p.Key) ?? [];
        var required = schema["required"]?.AsArray().Select(v => v!.GetValue<string>()) ?? [];
        var (names, requiredNames) = Fields(record);
        AssertSameSet(properties, names, $"{what}: fields of {record.Name}");
        AssertSameSet(required, requiredNames, $"{what}: required fields of {record.Name}");
    }

    // The serializer's own view of the record, so this checks what actually goes
    // on the wire rather than a guess from reflection.
    private static (HashSet<string> Names, HashSet<string> Required) Fields(Type record)
    {
        var info = ProtocolJson.Options.GetTypeInfo(record);
        return (info.Properties.Select(p => p.Name).ToHashSet(), info.Properties.Where(p => p.IsRequired).Select(p => p.Name).ToHashSet());
    }

    private static IEnumerable<string> WireNames(Type enumType) =>
        Enum.GetValues(enumType).Cast<object>().Select(v => JsonSerializer.Serialize(v, enumType, ProtocolJson.Options).Trim('"'));

    private static void AssertSameSet(IEnumerable<string> expected, IEnumerable<string> actual, string what)
    {
        var want = expected.Order(StringComparer.Ordinal).ToList();
        var have = actual.Order(StringComparer.Ordinal).ToList();
        Assert.True(want.SequenceEqual(have), $"{what} differ.\n  schema: {string.Join(", ", want)}\n  C#:     {string.Join(", ", have)}");
    }
}
