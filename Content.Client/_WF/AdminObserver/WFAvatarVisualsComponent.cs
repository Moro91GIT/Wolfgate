using Content.Shared._WF.AdminObserver;
using Robust.Shared.Prototypes;

namespace Content.Client._WF.AdminObserver;

/// <summary>What this client last drew a manifested observer as, so it redraws only when that changes.</summary>
[RegisterComponent]
public sealed partial class WFAvatarVisualsComponent : Component
{
    public WFAvatarLook? Look;

    /// <summary>Whether the observer's sprite is currently something other than its own.</summary>
    public bool Replaced;

    /// <summary>When a character avatar's sprite was last copied from its body.</summary>
    public TimeSpan LastCopy;
}

/// <summary>Everything that decides a manifested observer's sprite.</summary>
public readonly record struct WFAvatarLook(WFAvatarKind Kind, EntProtoId? Prototype, EntityUid? Body, bool Hologram);
