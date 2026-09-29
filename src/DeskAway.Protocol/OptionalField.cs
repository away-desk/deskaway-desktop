using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskAway.Protocol;

/// <summary>
/// A field that can be absent, which is different from present-and-null. The
/// protocol uses both: a heartbeat may send <c>{}</c> or
/// <c>{"currentItemId": null}</c>, and they must round-trip as sent. A plain
/// nullable cannot tell them apart. <c>default</c> is absent; mark the property
/// <c>[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]</c> so
/// an absent value is not written.
/// </summary>
[JsonConverter(typeof(OptionalFieldConverterFactory))]
public readonly record struct OptionalField<T>
{
    /// <summary>A present value, which may itself be null.</summary>
    public OptionalField(T value)
    {
        Value = value;
        IsPresent = true;
    }

    /// <summary>Whether the field was present on the wire.</summary>
    public bool IsPresent { get; }

    /// <summary>The value when present; <c>default</c> when absent.</summary>
    public T Value { get; }

    /// <summary>Wraps a present value.</summary>
    public static implicit operator OptionalField<T>(T value) => new(value);
}

internal sealed class OptionalFieldConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(OptionalField<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(OptionalFieldConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class OptionalFieldConverter<T> : JsonConverter<OptionalField<T>>
    {
        // Without this, a JSON null would never reach Read, and present-and-null
        // would be indistinguishable from absent.
        public override bool HandleNull => true;

        public override OptionalField<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<T>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, OptionalField<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
