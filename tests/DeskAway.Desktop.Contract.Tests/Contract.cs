using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeskAway.Protocol;

namespace DeskAway.Desktop.Contract.Tests;

/// <summary>One file from the protocol's examples/valid or examples/invalid.</summary>
public sealed record Example(string Name, string Direction, string Frame, JsonNode? Message, string? ExpectedCloseReason)
{
    /// <summary>The endpoint that checks it: the relay for inbound, the addressee for outbound.</summary>
    public Endpoint Self =>
        Direction == "inbound" ? Endpoint.Relay : JsonSerializer.Deserialize<Endpoint>(Message!["to"]!.ToJsonString(), ProtocolJson.Options);

    /// <inheritdoc />
    public override string ToString() => Name;
}

/// <summary>The protocol contract the build downloaded at the pinned commit.</summary>
public static class Contract
{
    private static readonly Assembly Self = typeof(Contract).Assembly;

    /// <summary>The pinned protocol commit.</summary>
    public static string Commit => Metadata("DeskAwayProtocolCommit");

    /// <summary>The root of the downloaded contract: schemas/, enums/, examples/.</summary>
    public static string Root => Metadata("DeskAwayProtocolRoot");

    /// <summary>The root of this repository.</summary>
    public static string RepoRoot => Metadata("RepoRoot");

    /// <summary>Every example file of one kind, in name order, read straight from the folder.</summary>
    public static string[] ExampleFiles(string kind) =>
        Directory.GetFiles(Path.Combine(Root, "examples", kind), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();

    /// <summary>The file names of every example of one kind.</summary>
    public static string[] ExampleNames(string kind) =>
        ExampleFiles(kind).Select(f => Path.GetFileName(f.AsSpan()).ToString()).ToArray();

    /// <summary>Every example of one kind.</summary>
    public static IReadOnlyList<Example> Examples(string kind) => ExampleFiles(kind).Select(Load).ToList();

    /// <summary>One example by file name.</summary>
    public static Example Example(string kind, string name) => Load(Path.Combine(Root, "examples", kind, name));

    /// <summary>A schema or enum file from the contract.</summary>
    public static JsonNode Json(string relativePath) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(Root, relativePath)))!;

    private static Example Load(string path)
    {
        var wrapper = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var message = wrapper["message"];
        var frame = wrapper["raw"]?.GetValue<string>() ?? message!.ToJsonString();
        return new Example(
            Path.GetFileName(path),
            wrapper["direction"]!.GetValue<string>(),
            frame,
            message,
            wrapper["expect"]?["closeReason"]?.GetValue<string>());
    }

    private static string Metadata(string key) =>
        Self.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == key).Value
        ?? throw new InvalidOperationException($"assembly metadata {key} is missing");
}
