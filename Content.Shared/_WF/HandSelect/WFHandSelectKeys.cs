using Robust.Shared.Input;

namespace Content.Shared._WF.HandSelect;

/// <summary>Keys that make a given hand the active one.</summary>
[KeyFunctions]
public static class WFHandSelectKeys
{
    /// <summary>Makes the first left hand active.</summary>
    public static readonly BoundKeyFunction WFSelectLeftHand = "WFSelectLeftHand";

    /// <summary>Makes the first right hand active.</summary>
    public static readonly BoundKeyFunction WFSelectRightHand = "WFSelectRightHand";
}
