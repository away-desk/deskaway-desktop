using System.Text.Json;

namespace DeskAway.Protocol;

// Written from deskaway-protocol/schemas/envelope.v1.json, field by field.
// Contract.Tests compares these properties, and which are required, with that
// schema on every build; a field added there fails the build here.

/// <summary>
/// The fields every envelope carries, in both directions. Only the two sealed
/// envelopes derive from it: <see cref="InboundEnvelope"/> has no relay block,
/// <see cref="OutboundEnvelope"/> must have one.
/// </summary>
public abstract record Envelope
{
    private protected Envelope()
    {
    }

    /// <summary>Identifies the message. A resend after reconnect keeps it, so a receiver drops ids it has handled.</summary>
    public required Guid Id { get; init; }

    /// <summary>Which payload schema applies.</summary>
    public required MessageType Type { get; init; }

    /// <summary>Version of the envelope's shape. Only 1 exists.</summary>
    public required int EnvelopeVersion { get; init; }

    /// <summary>The session, or null when the message was sent before one existed. Absent on the wire, never empty.</summary>
    public Guid? Session { get; init; }

    /// <summary>Where the message goes. The relay routes on this alone.</summary>
    public required Endpoint To { get; init; }

    /// <summary>The run, or <see cref="Guid.Empty"/> (the nil UUID) for a message outside any run.</summary>
    public required Guid RunId { get; init; }

    /// <summary>Correlates one user action across every hop.</summary>
    public required Guid TraceId { get; init; }

    /// <summary>The sender's clock. Informational only; <see cref="RelayBlock.ReceivedAt"/> is authoritative.</summary>
    public required UtcTimestamp SentAt { get; init; }

    /// <summary>The content, as received. Read it with <see cref="PayloadAs{T}"/>.</summary>
    public required JsonElement Payload { get; init; }

    /// <summary>The payload as its record type. Only meaningful after the checker has validated the payload.</summary>
    public T PayloadAs<T>() => Payload.Deserialize<T>(ProtocolJson.Options)!;
}

/// <summary>A message as the desktop sends it to the relay. It has no relay block.</summary>
public sealed record InboundEnvelope : Envelope;

/// <summary>A message as the relay delivers it: the envelope plus the block the relay stamped on it.</summary>
public sealed record OutboundEnvelope : Envelope
{
    /// <summary>Written by the relay. Never supplied by a sender.</summary>
    public required RelayBlock Relay { get; init; }
}

/// <summary>What the relay stamps on every message it delivers.</summary>
public sealed record RelayBlock
{
    /// <summary>The sender, from the authenticated connection it arrived on.</summary>
    public required Endpoint From { get; init; }

    /// <summary>The relay's clock: the authoritative time.</summary>
    public required UtcTimestamp ReceivedAt { get; init; }

    /// <summary>Position in this session's stream to this receiver, from 1.</summary>
    public required long Sequence { get; init; }
}
