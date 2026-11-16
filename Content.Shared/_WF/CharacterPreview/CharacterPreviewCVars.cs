using Robust.Shared.Configuration;

namespace Content.Shared._WF.CharacterPreview;

/// <summary>Settings for the character editor's preview.</summary>
[CVarDefs]
public sealed class CharacterPreviewCVars
{
    /// <summary>Which floor tile the preview character stands on; 0 is none. See <c>WFPreviewFrame.Backgrounds</c>.</summary>
    public static readonly CVarDef<int> Background =
        CVarDef.Create("wf.preview_background", 0, CVar.ARCHIVE | CVar.CLIENTONLY);
}
