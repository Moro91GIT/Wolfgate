using Content.Shared._WF.Screentips;

namespace Content.Client.Options.UI.Tabs;

/// <summary>The screentip settings in the Cursor section of the Misc tab.</summary>
public sealed partial class MiscTab
{
    private void WfAddScreentipOptions()
    {
        Control.AddOptionDropDown(
            ScreentipCVars.Mode,
            DropDownScreentips,
            new List<OptionDropDownCVar<int>.ValueOption>
            {
                new((int) ScreentipMode.Off, Loc.GetString("ui-options-wf-screentips-off")),
                new((int) ScreentipMode.TopOfView, Loc.GetString("ui-options-wf-screentips-top")),
                new((int) ScreentipMode.NextToCursor, Loc.GetString("ui-options-wf-screentips-cursor")),
            });
        Control.AddOptionColorSlider(ScreentipCVars.Color, ScreentipColorSlider);
    }
}
