namespace DeskAway.Protocol;

// Each enum mirrors a file in deskaway-protocol/enums/ and is serialized as its
// kebab-case name ("session-claim"). Contract.Tests fails if the C# values and
// the enum file ever differ, so a new protocol value breaks the build here.

/// <summary>Every envelope <c>type</c>. Mirrors <c>enums/message-type.json</c>.</summary>
public enum MessageType
{
    /// <summary><c>hello</c>: first message on every connection.</summary>
    Hello,
    /// <summary><c>heartbeat</c>: liveness, every 7 seconds.</summary>
    Heartbeat,
    /// <summary><c>goodbye</c>: a deliberate disconnect.</summary>
    Goodbye,
    /// <summary><c>session-claim</c>: a phone takes control of a desktop.</summary>
    SessionClaim,
    /// <summary><c>session-evicted</c>: the device lost its session to another claim.</summary>
    SessionEvicted,
}

/// <summary>Where a message comes from or goes to. Mirrors <c>enums/endpoint.json</c>.</summary>
public enum Endpoint
{
    /// <summary><c>desktop</c>.</summary>
    Desktop,
    /// <summary><c>phone</c>.</summary>
    Phone,
    /// <summary><c>relay</c>.</summary>
    Relay,
}

/// <summary>Why a connection was closed. Mirrors <c>enums/close-reason.json</c>.</summary>
public enum CloseReason
{
    /// <summary><c>normal</c>.</summary>
    Normal,
    /// <summary><c>evicted</c>.</summary>
    Evicted,
    /// <summary><c>message-too-large</c>: over 256 KiB, refused before parsing.</summary>
    MessageTooLarge,
    /// <summary><c>invalid-json</c>.</summary>
    InvalidJson,
    /// <summary><c>unsupported-envelope-version</c>.</summary>
    UnsupportedEnvelopeVersion,
    /// <summary><c>relay-fields-from-sender</c>: an inbound frame carried a relay block.</summary>
    RelayFieldsFromSender,
    /// <summary><c>unknown-type</c>.</summary>
    UnknownType,
    /// <summary><c>invalid-envelope</c>.</summary>
    InvalidEnvelope,
    /// <summary><c>invalid-payload</c>.</summary>
    InvalidPayload,
    /// <summary><c>device-mismatch</c>: hello's role or deviceId did not match the connection.</summary>
    DeviceMismatch,
}
