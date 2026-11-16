using Robust.Shared.Input;

namespace Content.Shared._WF.CancelInteractions;

/// <summary>The key that cancels the player's own interactions in progress.</summary>
[KeyFunctions]
public static class WFCancelInteractionKeys
{
    /// <summary>Cancels every do-after the player is performing.</summary>
    public static readonly BoundKeyFunction WFCancelInteractions = "WFCancelInteractions";
}
