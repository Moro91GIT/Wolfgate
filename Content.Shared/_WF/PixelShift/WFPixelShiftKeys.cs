using Robust.Shared.Input;

namespace Content.Shared._WF.PixelShift;

/// <summary>Pixel shift keys, bound in the human input context.</summary>
[KeyFunctions]
public static class WFPixelShiftKeys
{
    /// <summary>While held, the movement keys nudge your sprite a pixel instead of moving you.</summary>
    public static readonly BoundKeyFunction WFPixelShift = "WFPixelShift";

    /// <summary>While held, left and right tilt your sprite instead of moving you.</summary>
    public static readonly BoundKeyFunction WFPixelTilt = "WFPixelTilt";

    /// <summary>Puts your sprite back where it started, upright.</summary>
    public static readonly BoundKeyFunction WFPixelShiftReset = "WFPixelShiftReset";
}
