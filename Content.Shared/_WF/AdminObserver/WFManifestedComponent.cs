using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.AdminObserver;

/// <summary>
/// On an admin observer only while it is manifested: visible to living players as an avatar, and speaking in
/// character. Always comes with a <see cref="WFStaffMarkerComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class WFManifestedComponent : Component
{
    /// <summary>What the observer is shown as.</summary>
    [DataField, AutoNetworkedField]
    public WFAvatarKind Kind;

    /// <summary>The entity prototype whose sprite is shown, for object and mob avatars.</summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Prototype;

    /// <summary>The dressed humanoid whose sprite is shown, for character avatars.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>How the observer arrived, and how it leaves.</summary>
    [DataField, AutoNetworkedField]
    public WFEntryStyle EntryStyle;

    /// <summary>The saved preset this came from; empty for the plain ghost.</summary>
    [DataField]
    public string Preset = string.Empty;

    /// <summary>The observer's name before it manifested, put back when it hides.</summary>
    [DataField]
    public string PreviousName = string.Empty;

    /// <summary>The observer's description before it manifested, put back when it hides.</summary>
    [DataField]
    public string PreviousDescription = string.Empty;

    /// <summary>Whether manifesting added the radio transmitter, so hiding removes it.</summary>
    [DataField]
    public bool AddedTransmitter;

    /// <summary>Whether manifesting added the telecom exemption, so hiding removes it.</summary>
    [DataField]
    public bool AddedTelecomExempt;
}

/// <summary>Raised on an admin observer by its Manifest action: show it, or hide it again.</summary>
public sealed partial class WFToggleManifestEvent : InstantActionEvent;
