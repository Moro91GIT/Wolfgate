using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.AdminTech;

/// <summary>
/// On a body spawned with Spawn as Outfit whose player was given Return: an action that deletes the body and puts
/// the player back where they were, in their earlier body or observing.
/// </summary>
[RegisterComponent]
public sealed partial class WFReturnToObserverComponent : Component
{
    /// <summary>The body the player's mind owned before this one, if it had one.</summary>
    [ViewVariables]
    public EntityUid? PreviousBody;

    [DataField]
    public EntProtoId Action = "WFActionReturn";

    [ViewVariables]
    public EntityUid? ActionEntity;
}

/// <summary>Raised on a spawned body by its Return action.</summary>
public sealed partial class WFReturnToObserverEvent : InstantActionEvent;
