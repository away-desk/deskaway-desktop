using System.Xml.Linq;

namespace DeskAway.Desktop.Contract.Tests;

/// <summary>
/// DeskAway.Protocol holds wire frames. A frame is translated into a domain
/// object at the Transport edge and never reaches Core or Execution
/// (docs/architecture.md), so only Transport may reference it — and it
/// references nothing in this solution itself. This makes that a failing
/// test rather than a convention.
/// </summary>
public sealed class ReferenceDirectionTests
{
    private static readonly string[] AllowedToReferenceProtocol = ["DeskAway.Desktop.Transport"];

    [Fact]
    public void Protocol_references_no_other_project()
    {
        var protocol = Projects().Single(p => p.Name == "DeskAway.Protocol");
        Assert.Empty(protocol.References);
    }

    [Fact]
    public void Only_Transport_references_Protocol()
    {
        var offenders = Projects()
            .Where(p => p.References.Contains("DeskAway.Protocol") && !AllowedToReferenceProtocol.Contains(p.Name))
            .Select(p => p.Name)
            .ToList();

        Assert.True(offenders.Count == 0, $"only {string.Join(", ", AllowedToReferenceProtocol)} may reference DeskAway.Protocol; also referenced by: {string.Join(", ", offenders)}");
    }

    private static IEnumerable<(string Name, HashSet<string> References)> Projects() =>
        Directory.GetFiles(Path.Combine(Contract.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => (
                Path.GetFileNameWithoutExtension(path),
                XDocument.Load(path).Descendants("ProjectReference")
                    .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
                    .ToHashSet()));
}
