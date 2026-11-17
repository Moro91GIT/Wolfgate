using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;

namespace Content.Client._WF.Wilderness;

/// <summary>The viewport in the coordinates a screen-space overlay draws in.</summary>
internal static class WFZoneScreen
{
    // ViewportBounds is in global physical pixels, but the handle is already at the viewport's top-left.
    public static UIBox2 Of(in OverlayDrawArgs args)
    {
        var origin = (args.ViewportControl as Control)?.GlobalPixelPosition ?? Vector2i.Zero;
        var bounds = args.ViewportBounds;
        return new UIBox2(bounds.Left - origin.X, bounds.Top - origin.Y, bounds.Right - origin.X, bounds.Bottom - origin.Y);
    }
}

/// <summary>A coloured frame round the game view, fading inward, that pulses unless motion is reduced.</summary>
public sealed class WFZoneBorderOverlay : Overlay
{
    private const int Bands = 14;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public Color Color = Color.Red;

    /// <summary>0 to 1; drawn only above zero.</summary>
    public float Alpha;

    protected override bool BeforeDraw(in OverlayDrawArgs args) => Alpha > 0.001f;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var box = WFZoneScreen.Of(args);
        var band = MathF.Max(2f, MathF.Min(box.Width, box.Height) * 0.006f);
        for (var i = 0; i < Bands; i++)
        {
            var inset = band * i;
            var fade = 1f - i / (float) Bands;
            var color = Color.WithAlpha(Alpha * 0.55f * fade * fade);
            var outer = new UIBox2(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset);
            handle.DrawRect(new UIBox2(outer.Left, outer.Top, outer.Right, outer.Top + band), color);
            handle.DrawRect(new UIBox2(outer.Left, outer.Bottom - band, outer.Right, outer.Bottom), color);
            handle.DrawRect(new UIBox2(outer.Left, outer.Top + band, outer.Left + band, outer.Bottom - band), color);
            handle.DrawRect(new UIBox2(outer.Right - band, outer.Top + band, outer.Right, outer.Bottom - band), color);
        }
    }
}

/// <summary>A large title across the upper middle of the game view on crossing into or out of a zone.</summary>
public sealed class WFZoneTitleOverlay : Overlay
{
    [Dependency] private IResourceCache _cache = default!;

    private const int FontSize = 36;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private readonly Font _font;
    private readonly Font _subFont;

    public string Title = string.Empty;
    public string Sub = string.Empty;
    public Color Color = Color.White;
    public float Alpha;

    public WFZoneTitleOverlay()
    {
        IoCManager.InjectDependencies(this);
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), FontSize);
        _subFont = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), FontSize / 3);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => Alpha > 0.001f && Title.Length > 0;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;
        var box = WFZoneScreen.Of(args);
        var scale = Math.Clamp(box.Height / 1080f, 0.5f, 1.2f);
        var centre = box.Left + box.Width / 2f;
        var y = box.Top + box.Height * 0.22f;

        var size = handle.GetDimensions(_font, Title, scale);
        // Narrow views shrink the title to fit rather than cut it off.
        var widest = box.Width * 0.7f;
        if (size.X > widest)
        {
            scale *= widest / size.X;
            size = handle.GetDimensions(_font, Title, scale);
        }

        var subSize = Sub.Length > 0 ? handle.GetDimensions(_subFont, Sub, scale) : Vector2.Zero;
        var width = MathF.Max(size.X, subSize.X) + 48f * scale;
        var back = new UIBox2(centre - width / 2f, y - 12f * scale, centre + width / 2f, y + size.Y + subSize.Y + 16f * scale);
        handle.DrawRect(back, Color.Black.WithAlpha(0.6f * Alpha));
        handle.DrawRect(new UIBox2(back.Left, back.Top, back.Right, back.Top + 4f * scale), Color.WithAlpha(Alpha));
        handle.DrawRect(new UIBox2(back.Left, back.Bottom - 4f * scale, back.Right, back.Bottom), Color.WithAlpha(Alpha));

        handle.DrawString(_font, new Vector2(centre - size.X / 2f, y), Title, scale, Color.WithAlpha(Alpha));
        if (Sub.Length > 0)
            handle.DrawString(_subFont, new Vector2(centre - subSize.X / 2f, y + size.Y), Sub, scale, Color.White.WithAlpha(Alpha * 0.9f));
    }
}
