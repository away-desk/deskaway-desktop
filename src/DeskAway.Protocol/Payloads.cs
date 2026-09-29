using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace DeskAway.Protocol;

// Written from deskaway-protocol/schemas/control/*.v1.json, field by field.
// Contract.Tests compares each record's properties, and which are required,
// with its schema on every build.

/// <summary><c>hello</c>'s <c>role</c>.</summary>
public enum HelloRole
{
    /// <summary><c>desktop</c>.</summary>
    Desktop,
    /// <summary><c>phone</c>.</summary>
    Phone,
}

/// <summary>
/// First message on every connection. <see cref="Role"/> and <see cref="DeviceId"/>
/// are verified by the relay; everything else is the device's own claim.
/// </summary>
public sealed record HelloPayload
{
    /// <summary>Verified against the authenticated connection.</summary>
    public required HelloRole Role { get; init; }

    /// <summary>Verified against the authenticated connection.</summary>
    public required Guid DeviceId { get; init; }

    /// <summary>Self-reported.</summary>
    public required string DeviceName { get; init; }

    /// <summary>Self-reported.</summary>
    public required string Os { get; init; }

    /// <summary>Self-reported. SemVer.</summary>
    public required string AppVersion { get; init; }

    /// <summary>Desktop only, self-reported: the folder the desktop says it will confine work to. Not a guarantee.</summary>
    public string? Folder { get; init; }

    /// <summary>Desktop only, self-reported.</summary>
    public int? ScopeVersion { get; init; }

    /// <summary>The last relay sequence seen in the session being resumed; absent on a first connection.</summary>
    public long? LastSeenSequence { get; init; }
}

/// <summary>Liveness, with what the sender is working on.</summary>
public sealed record HeartbeatPayload
{
    /// <summary>The checklist item in progress. Absent, null, or an id — all three are distinct on the wire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalField<Guid?> CurrentItemId { get; init; }

    /// <summary>The step in progress. Absent, null, or an id — all three are distinct on the wire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public OptionalField<Guid?> CurrentStepId { get; init; }
}

/// <summary>A deliberate disconnect. No fields.</summary>
public sealed record GoodbyePayload;

/// <summary>A phone takes control of a desktop.</summary>
public sealed record SessionClaimPayload
{
    /// <summary>The desktop being claimed.</summary>
    public required Guid DesktopDeviceId { get; init; }
}

/// <summary>The device lost its session to another claim. No fields.</summary>
public sealed record SessionEvictedPayload;

/// <summary>The payload record for each message type.</summary>
public static class PayloadTypes
{
    /// <summary>Message type to payload record. Contract.Tests requires an entry for every message type.</summary>
    public static FrozenDictionary<MessageType, Type> ByMessageType { get; } = new Dictionary<MessageType, Type>
    {
        [MessageType.Hello] = typeof(HelloPayload),
        [MessageType.Heartbeat] = typeof(HeartbeatPayload),
        [MessageType.Goodbye] = typeof(GoodbyePayload),
        [MessageType.SessionClaim] = typeof(SessionClaimPayload),
        [MessageType.SessionEvicted] = typeof(SessionEvictedPayload),
    }.ToFrozenDictionary();
}
