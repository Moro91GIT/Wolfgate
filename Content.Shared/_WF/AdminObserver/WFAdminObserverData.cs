using Robust.Shared.Serialization;

namespace Content.Shared._WF.AdminObserver;

/// <summary>
/// Everything an admin's observer keeps between rounds: avatar presets, the staff marker and the kit layout. One
/// per admin, stored as JSON in the database and sent to that admin's client.
/// </summary>
[Serializable, NetSerializable]
public sealed class WFAdminObserverData
{
    public const int CurrentVersion = 1;

    /// <summary>The largest document the server stores, as JSON.</summary>
    public const int MaxBytes = 64 * 1024;

    public const int MaxPresets = 32;
    public const int MaxNameLength = 32;
    public const int MaxDisplayNameLength = 64;
    public const int MaxDescriptionLength = 512;

    /// <summary>The most items a layout holds, counting contents.</summary>
    public const int MaxKitNodes = 200;

    /// <summary>How deep a layout goes: a bag, a pouch in it, an item in that.</summary>
    public const int MaxKitDepth = 3;

    /// <summary>The format's version, for reading older documents.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>The avatars the admin can manifest as.</summary>
    public List<WFAvatarPreset> Presets { get; set; } = new();

    /// <summary>The name of the preset a plain manifest uses.</summary>
    public string? DefaultPreset { get; set; }

    /// <summary>How the admin is marked as staff while visible.</summary>
    public WFStaffMarkerSettings Marker { get; set; } = new();

    /// <summary>What the observer carries, or null for the default kit.</summary>
    public WFKitLayout? Kit { get; set; }
}

/// <summary>What a manifested observer looks like.</summary>
[Serializable, NetSerializable]
public enum WFAvatarKind : byte
{
    /// <summary>The observer's own ghost sprite.</summary>
    Ghost,

    /// <summary>An item or structure prototype's sprite.</summary>
    Object,

    /// <summary>A mob prototype's sprite.</summary>
    Mob,

    /// <summary>One of the admin's saved characters in an outfit.</summary>
    Character,
}

/// <summary>How a manifested observer arrives and leaves.</summary>
[Serializable, NetSerializable]
public enum WFEntryStyle : byte
{
    Bluespace,
    Silent,
}

/// <summary>One avatar an admin can manifest as.</summary>
[Serializable, NetSerializable]
public sealed class WFAvatarPreset
{
    /// <summary>The preset's name in the admin's list; unique per admin.</summary>
    public string Name { get; set; } = string.Empty;

    public WFAvatarKind Kind { get; set; }

    /// <summary>The entity prototype shown, for <see cref="WFAvatarKind.Object"/> and <see cref="WFAvatarKind.Mob"/>.</summary>
    public string? Prototype { get; set; }

    /// <summary>The character slot shown, for <see cref="WFAvatarKind.Character"/>.</summary>
    public int? CharacterSlot { get; set; }

    /// <summary>The starting gear the character wears, when no layout is saved.</summary>
    public string? Outfit { get; set; }

    /// <summary>What the character wears as dressed by hand; wins over <see cref="Outfit"/>.</summary>
    public WFKitLayout? OutfitLayout { get; set; }

    /// <summary>The name players see; empty for the avatar's own.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The examine text players see; empty for the avatar's own.</summary>
    public string Description { get; set; } = string.Empty;

    public WFEntryStyle EntryStyle { get; set; }
}

/// <summary>What the staff marker's label says.</summary>
[Serializable, NetSerializable]
public enum WFMarkerWording : byte
{
    /// <summary>The admin's rank title.</summary>
    Rank,

    /// <summary>The word "Staff".</summary>
    Staff,

    /// <summary>The admin's account name.</summary>
    Name,

    /// <summary>Rank and account name together.</summary>
    RankAndName,
}

/// <summary>The visible parts of a staff marker. At least one is always on.</summary>
[Flags]
[Serializable, NetSerializable]
public enum WFMarkerVisuals : byte
{
    None = 0,
    Nameplate = 1 << 0,
    Sigil = 1 << 1,
    Aura = 1 << 2,
    Shield = 1 << 3,
    Hologram = 1 << 4,
    Glyph = 1 << 5,
    Ring = 1 << 6,
}

/// <summary>An admin's choices for their staff marker.</summary>
[Serializable, NetSerializable]
public sealed class WFStaffMarkerSettings
{
    public WFMarkerWording Wording { get; set; } = WFMarkerWording.Rank;

    public WFMarkerVisuals Visuals { get; set; } = WFMarkerVisuals.Nameplate;

    /// <summary>The chosen look of each visual that has several; empty for the default.</summary>
    public string Ring { get; set; } = string.Empty;

    /// <inheritdoc cref="Ring"/>
    public string Glyph { get; set; } = string.Empty;

    /// <inheritdoc cref="Ring"/>
    public string Shield { get; set; } = string.Empty;

    /// <inheritdoc cref="Ring"/>
    public string Aura { get; set; } = string.Empty;

    /// <summary>The marker's colour as hex. Only honoured for holders of the Host flag.</summary>
    public string? Color { get; set; }
}

/// <summary>What an entity wears and holds, by item prototype and position.</summary>
[Serializable, NetSerializable]
public sealed class WFKitLayout
{
    /// <summary>The item in each inventory slot, by slot name.</summary>
    public Dictionary<string, WFKitNode> Slots { get; set; } = new();

    /// <summary>The item in each hand, by hand name.</summary>
    public Dictionary<string, WFKitNode> Hands { get; set; } = new();
}

/// <summary>One item of a layout and what its storage grid holds.</summary>
[Serializable, NetSerializable]
public sealed class WFKitNode
{
    public string Prototype { get; set; } = string.Empty;

    public List<WFKitStored> Contents { get; set; } = new();
}

/// <summary>An item inside another's storage grid.</summary>
[Serializable, NetSerializable]
public sealed class WFKitStored
{
    public WFKitNode Node { get; set; } = new();

    public int X { get; set; }

    public int Y { get; set; }

    /// <summary>The item's rotation in the grid, in degrees.</summary>
    public int Rotation { get; set; }
}
