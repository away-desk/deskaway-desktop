using System.Text.Json;
using System.Text.Json.Nodes;
using DeskAway.Protocol;

namespace DeskAway.Desktop.Contract.Tests;

/// <summary>
/// Every example the pinned protocol ships, each as its own test case — never a
/// sample. The same examples run through the JavaScript and Python checkers in
/// deskaway-protocol; all three must agree.
/// </summary>
public sealed class ExampleTests
{
    private static readonly MessageChecker Checker = MessageChecker.Create();

    /// <summary>File names in examples/valid.</summary>
    public static TheoryData<string> Valid => [.. Contract.ExampleNames("valid")];

    /// <summary>File names in examples/invalid.</summary>
    public static TheoryData<string> Invalid => [.. Contract.ExampleNames("invalid")];

    [Theory]
    [MemberData(nameof(Valid))]
    public void Valid_example_passes_the_checker_and_round_trips_through_the_typed_records(string name)
    {
        var example = Contract.Example("valid", name);

        Envelope message;
        if (example.Direction == "inbound")
        {
            var result = Checker.CheckInbound(example.Frame, example.Self, PayloadCheck.Always);
            Assert.True(result.Ok, $"{name}: closed with {result.CloseReason} ({result.Detail})");
            message = result.Message!;
        }
        else
        {
            var result = Checker.CheckOutbound(example.Frame, example.Self, PayloadCheck.Always);
            Assert.True(result.Ok, $"{name}: closed with {result.CloseReason} ({result.Detail})");
            message = result.Message!;
        }

        // The envelope, written back out, is the JSON that came in.
        var envelope = JsonSerializer.SerializeToNode(message, message.GetType(), ProtocolJson.Options);
        AssertSameJson(example.Message!, envelope, $"{name}: envelope");

        // The payload, read into its record and written back out, is the payload
        // that came in. This is what proves the hand-written payload record is
        // faithful: a renamed, missing or mistyped field breaks it.
        var payloadType = PayloadTypes.ByMessageType[message.Type];
        var typed = message.Payload.Deserialize(payloadType, ProtocolJson.Options);
        var payload = JsonSerializer.SerializeToNode(typed, payloadType, ProtocolJson.Options);
        AssertSameJson(example.Message!["payload"]!, payload, $"{name}: payload as {payloadType.Name}");
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public void Invalid_example_is_refused_with_its_expected_close_reason(string name)
    {
        var example = Contract.Example("invalid", name);
        var expected = JsonSerializer.Deserialize<CloseReason>($"\"{example.ExpectedCloseReason}\"", ProtocolJson.Options);

        var actual = example.Direction == "inbound"
            ? Checker.CheckInbound(example.Frame, example.Self, PayloadCheck.Always).CloseReason
            : Checker.CheckOutbound(example.Frame, example.Self, PayloadCheck.Always).CloseReason;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Every_example_in_the_contract_became_a_test_case()
    {
        // Counted straight from the downloaded folders, independently of the
        // theory data, so the suite cannot pass by running nothing or a subset.
        var validFiles = Directory.GetFiles(Path.Combine(Contract.Root, "examples", "valid"), "*.json", SearchOption.AllDirectories).Length;
        var invalidFiles = Directory.GetFiles(Path.Combine(Contract.Root, "examples", "invalid"), "*.json", SearchOption.AllDirectories).Length;

        Assert.True(validFiles > 0, "no valid examples were downloaded");
        Assert.True(invalidFiles > 0, "no invalid examples were downloaded");
        Assert.Equal(validFiles, Valid.Count);
        Assert.Equal(invalidFiles, Invalid.Count);

        TestContext.Current.TestOutputHelper?.WriteLine(
            $"protocol {Contract.Commit}: {validFiles} valid and {invalidFiles} invalid examples checked (c#)");
    }

    private static void AssertSameJson(JsonNode expected, JsonNode? actual, string what) =>
        Assert.True(JsonNode.DeepEquals(expected, actual), $"{what} did not round-trip.\n expected: {expected.ToJsonString()}\n   actual: {actual?.ToJsonString()}");
}
