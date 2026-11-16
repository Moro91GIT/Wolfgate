using Robust.Shared.Input;

namespace Content.Shared._WF.TurnInPlace;

/// <summary>The turn-in-place key, bound in the human input context.</summary>
[KeyFunctions]
public static class WFTurnInPlaceKeys
{
    /// <summary>While held, the movement keys only turn the mob.</summary>
    public static readonly BoundKeyFunction WFTurnInPlace = "WFTurnInPlace";
}
