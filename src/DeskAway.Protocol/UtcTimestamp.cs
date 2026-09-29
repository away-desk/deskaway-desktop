using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskAway.Protocol;

/// <summary>
/// An RFC 3339 UTC timestamp as it appears on the wire, ending in <c>Z</c>.
/// Kept as its original text, so a message re-serializes exactly as it arrived;
/// convert with <see cref="ToDateTimeOffset"/> when a real time is needed.
/// The checker has already enforced the format before one of these exists.
/// </summary>
[JsonConverter(typeof(UtcTimestampConverter))]
public readonly record struct UtcTimestamp
{
    /// <summary>Wraps wire text. Use <see cref="From"/> to create one from a time.</summary>
    public UtcTimestamp(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));

    /// <summary>The timestamp exactly as on the wire.</summary>
    public string Value { get; }

    /// <summary>The instant this timestamp names, in UTC.</summary>
    public DateTimeOffset ToDateTimeOffset() =>
        DateTimeOffset.Parse(Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    /// <summary>The canonical wire form of an instant: UTC, millisecond precision, <c>Z</c>.</summary>
    public static UtcTimestamp From(DateTimeOffset instant) =>
        new(instant.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public override string ToString() => Value;
}

internal sealed class UtcTimestampConverter : JsonConverter<UtcTimestamp>
{
    public override UtcTimestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? new UtcTimestamp(reader.GetString()!)
            : throw new JsonException("A timestamp must be a JSON string.");

    public override void Write(Utf8JsonWriter writer, UtcTimestamp value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
