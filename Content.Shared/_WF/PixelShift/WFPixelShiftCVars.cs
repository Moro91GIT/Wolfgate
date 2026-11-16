using Robust.Shared.Configuration;

namespace Content.Shared._WF.PixelShift;

/// <summary>Pixel shift limits. Replicated so the client predicts the same clamp.</summary>
[CVarDefs]
public sealed class WFPixelShiftCVars
{
    /// <summary>
    /// Furthest a sprite can be shifted on each axis, in pixels. 16 is half a tile, so a shifted mob still reads as
    /// standing on its own tile.
    /// </summary>
    public static readonly CVarDef<int> MaxOffset =
        CVarDef.Create("wf.pixel_shift_max_offset", 16, CVar.SERVER | CVar.REPLICATED);

    /// <summary>Furthest a sprite can be tilted either way, in degrees. 45 leans well short of lying down at 90.</summary>
    public static readonly CVarDef<int> MaxTilt =
        CVarDef.Create("wf.pixel_shift_max_tilt", 45, CVar.SERVER | CVar.REPLICATED);
}
