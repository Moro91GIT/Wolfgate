using Robust.Shared.Configuration;

namespace Content.Shared._WF.DarkFlash;

/// <summary>Photosensitivity settings for screen flashes.</summary>
[CVarDefs]
public sealed class DarkFlashCVars
{
    /// <summary>
    /// Bright screen flashes go dark instead of white, and being flashed fades without the spinning after-images.
    /// </summary>
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("wf.dark_flash", false, CVar.ARCHIVE | CVar.CLIENTONLY);
}
