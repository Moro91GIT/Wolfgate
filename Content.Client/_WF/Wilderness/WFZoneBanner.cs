using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client._WF.Wilderness;

/// <summary>
/// The banner at the top of the screen while in a zone or marked: its icon, title and what it means, and the mark's
/// countdown. Click it for the rule.
/// </summary>
public sealed class WFZoneBanner : PanelContainer
{
    private readonly StyleBoxFlat _box = new();
    private readonly TextureRect _icon;
    private readonly Label _title;
    private readonly RichTextLabel _summary;
    private readonly Label _mark;
    private readonly Label _hint;
    private readonly WFHazardStripe _top;
    private readonly WFHazardStripe _bottom;

    private Color _color = Color.White;

    /// <summary>Raised when the banner is clicked.</summary>
    public event Action? Clicked;

    public WFZoneBanner()
    {
        var cache = IoCManager.Resolve<IResourceCache>();
        var bold = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 18);
        var markFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 13);
        var small = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);

        MouseFilter = MouseFilterMode.Stop;
        MaxWidth = 640;
        PanelOverride = _box;
        _box.BackgroundColor = Color.Black.WithAlpha(0.85f);
        _box.BorderThickness = new Thickness(3);

        _top = new WFHazardStripe { MinHeight = 6 };
        _bottom = new WFHazardStripe { MinHeight = 6 };
        _icon = new TextureRect
        {
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            TextureScale = new Vector2(1.5f),
            MinSize = new Vector2(48, 48),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
        };
        _title = new Label { FontOverride = bold };
        _summary = new RichTextLabel { HorizontalExpand = true };
        _mark = new Label { FontOverride = markFont, FontColorOverride = Color.FromHex("#ff5a50"), Visible = false };
        _hint = new Label { FontOverride = small, FontColorOverride = Color.FromHex("#9aa4ad"), Text = Loc.GetString("wf-zone-banner-hint") };

        var text = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        text.AddChild(_title);
        text.AddChild(_summary);
        text.AddChild(_mark);
        text.AddChild(_hint);

        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, Margin = new Thickness(10, 6) };
        row.AddChild(_icon);
        row.AddChild(text);

        var column = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        column.AddChild(_top);
        column.AddChild(row);
        column.AddChild(_bottom);
        AddChild(column);
    }

    /// <summary>Fills the banner. <paramref name="mark"/> is the mark line, or null for none.</summary>
    public void Set(Color color, Texture? icon, string title, string summary, string? mark, bool stripes, bool hint)
    {
        _color = color;
        _box.BorderColor = color;
        _icon.Texture = icon;
        _icon.Visible = icon != null;
        _title.Text = title;
        _title.FontColorOverride = color;

        var message = new FormattedMessage();
        message.AddText(summary);
        _summary.SetMessage(message);

        _mark.Text = mark ?? string.Empty;
        _mark.Visible = mark != null;
        _hint.Visible = hint;
        _top.Visible = stripes;
        _bottom.Visible = stripes;
        _top.Color = color;
        _bottom.Color = color;
    }

    /// <summary>Sets how strongly the border shows, for pulsing.</summary>
    public void Pulse(float strength)
    {
        _box.BorderColor = _color.WithAlpha(Math.Clamp(strength, 0f, 1f));
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        Clicked?.Invoke();
        args.Handle();
    }
}

/// <summary>A strip of diagonal hazard stripes in a colour and black.</summary>
public sealed class WFHazardStripe : Control
{
    public Color Color = Color.Red;

    private readonly Vector2[] _quad = new Vector2[4];

    public WFHazardStripe()
    {
        RectClipContent = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), Color.Black);
        var height = (float) size.Y;
        var step = height * 2f;
        for (var x = -height; x < size.X; x += step * 2f)
        {
            _quad[0] = new Vector2(x, height);
            _quad[1] = new Vector2(x + height, 0f);
            _quad[2] = new Vector2(x + height + step, 0f);
            _quad[3] = new Vector2(x + step, height);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _quad, Color);
        }
    }
}
