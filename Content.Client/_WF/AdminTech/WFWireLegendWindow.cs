using System.Numerics;
using Content.Shared._WF.AdminTech;
using Content.Shared.Wires;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._WF.AdminTech;

/// <summary>Lists what each wire of an open wires panel does, by colour and letter, beside the panel.</summary>
public sealed class WFWireLegendWindow : DefaultWindow
{
    private readonly BoxContainer _rows;

    public WFWireLegendWindow()
    {
        Title = Loc.GetString("wf-admin-tech-wire-legend");
        MinSize = new Vector2(220, 120);
        _rows = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4, Margin = new Thickness(6) };
        Contents.AddChild(_rows);
    }

    /// <summary>Fills the list from what the server sent and opens the window if it is not open.</summary>
    public void Show(WFWireLegendMessage legend)
    {
        _rows.RemoveAllChildren();
        foreach (var wire in legend.Wires)
        {
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };
            row.AddChild(new PanelContainer
            {
                MinSize = new Vector2(14, 14),
                VerticalAlignment = Control.VAlignment.Center,
                PanelOverride = new StyleBoxFlat { BackgroundColor = wire.Color.ColorValue() },
            });
            row.AddChild(new Label { Text = wire.Letter.Letter().ToString(), MinSize = new Vector2(14, 0) });
            row.AddChild(new Label { Text = wire.Name, HorizontalExpand = true });
            _rows.AddChild(row);
        }

        if (!IsOpen)
            OpenCenteredLeft();
    }
}
