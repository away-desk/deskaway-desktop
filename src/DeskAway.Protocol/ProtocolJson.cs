using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskAway.Protocol;

/// <summary>The one set of JSON options for everything on the wire.</summary>
public static class ProtocolJson
{
    /// <summary>
    /// camelCase names, matched case-sensitively; unknown fields, duplicate
    /// fields, missing required fields and nulls where none are allowed all
    /// fail; enums as kebab-case strings, never numbers; absent optional fields
    /// are left out rather than written as null.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            AllowDuplicateProperties = false,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
