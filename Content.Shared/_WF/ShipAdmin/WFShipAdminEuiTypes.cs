using System.Numerics;
using Content.Shared._WF.ShipAccess;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.ShipAdmin;

/// <summary>Console command names for the admin ship panel.</summary>
public static class WFShipAdminCommands
{
    public const string Panel = "shipadmin";
}

/// <summary>Which grids the panel lists.</summary>
[Serializable, NetSerializable]
public enum WFShipAdminFilter : byte
{
    /// <summary>Grids with a deed, an owner, ship access or a vessel design.</summary>
    PlayerShips,
    AllGrids,
}

/// <summary>Whether a ship has an owner and whether any of them is connected.</summary>
[Serializable, NetSerializable]
public enum WFShipAdminOwnerState : byte
{
    None,
    Online,
    Offline,
}

/// <summary>What a list row shows as small tags.</summary>
[Flags, Serializable, NetSerializable]
public enum WFShipAdminRowFlags : ushort
{
    None = 0,
    Locked = 1 << 0,
    Docked = 1 << 1,
    InFtl = 1 << 2,
    Snapshot = 1 << 3,
    CrewLinked = 1 << 4,
    Company = 1 << 5,
    Hangar = 1 << 6,
    Fixing = 1 << 7,
}

/// <summary>One ship in the panel's list.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminRow
{
    public NetEntity Grid;
    public string Name = string.Empty;
    public string Design = string.Empty;
    public string Owner = string.Empty;
    public WFShipAdminOwnerState OwnerState;
    public string Map = string.Empty;
    public int Occupants;
    public WFShipAdminRowFlags Flags;
}

/// <summary>How someone owns a ship.</summary>
[Serializable, NetSerializable]
public enum WFShipAdminOwnerKind : byte
{
    /// <summary>An ID card holding the deed.</summary>
    DeedCard,

    /// <summary>A voucher or other item holding the deed.</summary>
    Voucher,

    /// <summary>A player's account, whatever body they play.</summary>
    Account,
}

/// <summary>One owner of the selected ship.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminOwner
{
    public WFShipAdminOwnerKind Kind;
    public string Name = string.Empty;

    /// <summary>The account, when it is known: always for <see cref="WFShipAdminOwnerKind.Account"/>, for a card when it was issued to a player.</summary>
    public NetUserId? User;
    public string Username = string.Empty;
    public bool Online;

    /// <summary>The card or voucher, for the deed kinds.</summary>
    public NetEntity? Card;
}

/// <summary>Someone aboard the selected ship.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminOccupant
{
    public string Name = string.Empty;
    public string Username = string.Empty;
}

/// <summary>Everything the panel shows about the selected ship.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminDetail
{
    public NetEntity Grid;
    public string Name = string.Empty;
    public string Design = string.Empty;
    public string Map = string.Empty;
    public Vector2 Position;
    public float Mass;
    public WFShipAdminRowFlags Flags;
    public List<string> DockedTo = new();

    public List<WFShipAdminOwner> Owners = new();

    /// <summary>The owner name on the deed and shuttle record.</summary>
    public string RecordOwner = string.Empty;

    /// <summary>The account the shipyard registered the ship to, and whether it is connected.</summary>
    public string Registered = string.Empty;
    public bool RegisteredOnline;

    public bool HasAccess;
    public WFShipAccessMode Mode;
    public bool Locked;
    public bool CanLock;
    public List<WFShipAccessEntry> AllowList = new();
    public string Crew = string.Empty;
    public string Company = string.Empty;
    public string Faction = string.Empty;

    public List<WFShipAdminOccupant> Occupants = new();
    public int OccupantCount;

    /// <summary>Appraised value, filled by a scan.</summary>
    public int? Value;
}

/// <summary>The panel's state. The list is only sent when it was rebuilt; null keeps the client's copy.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminEuiState : EuiStateBase
{
    public List<WFShipAdminRow>? Ships;
    public bool Truncated;
    public WFShipAdminFilter Filter;
    public WFShipAdminDetail? Selected;
    public AdminFlags ViewerFlags;

    /// <summary>The outcome of the viewer's last action, and a counter that changes with each one.</summary>
    public string Result = string.Empty;
    public bool ResultError;
    public int ResultSeq;
}

/// <summary>Selects a ship, or none.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminSelectMessage(NetEntity? grid) : EuiMessageBase
{
    public readonly NetEntity? Grid = grid;
}

/// <summary>Rebuilds the list with a filter.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminListMessage(WFShipAdminFilter filter) : EuiMessageBase
{
    public readonly WFShipAdminFilter Filter = filter;
}

/// <summary>An action on a ship that needs no more than a switch.</summary>
[Serializable, NetSerializable]
public sealed class WFShipAdminActionMessage(NetEntity grid, WFShipAdminAction action, bool value = false, bool notify = false) : EuiMessageBase
{
    public readonly NetEntity Grid = grid;
    public readonly WFShipAdminAction Action = action;
    public readonly bool Value = value;
    public readonly bool Notify = notify;
}
