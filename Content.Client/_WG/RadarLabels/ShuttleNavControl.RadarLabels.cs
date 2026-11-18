using System.Numerics;
using Content.Client._WG.RadarLabels;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Collects every tag the scope draws, then lays them out together (<see cref="WGRadarTagLayout"/>): a tag sits by
/// its contact while it has room, and tags that would overlap share one stacked list, each row with a faint line to
/// its contact, until zooming in makes room for them again.
/// </summary>
public partial class ShuttleNavControl
{
    private static readonly Color WgStackBacking = Color.Black.WithAlpha(0.55f);
    private static readonly Color WgStackMoreColor = Color.FromHex("#9aa4ad");

    private readonly WGRadarTagLayout _wgTagLayout = new();
    private readonly List<WGRadarTagLayout.Tag> _wgTags = new();
    private readonly List<WgTagDraw> _wgTagDraws = new();
    private readonly List<WgTagText> _wgTagLines = new();
    private readonly List<WgTagText> _wgTagAbove = new();

    private readonly record struct WgTagText(string Text, float Scale, Color Color, Vector2 Size);

    /// <summary>How a tag draws: its lines, anything drawn after its first line, and anything drawn only when it is alone.</summary>
    private struct WgTagDraw
    {
        public int FirstLine;
        public int LineCount;
        public int FirstAbove;
        public int AboveCount;
        public float HeadTextWidth;
        public Action<DrawingHandleScreen, Vector2>? DrawHeadExtra;
        public Action<DrawingHandleScreen, Vector2>? DrawAlone;
    }

    /// <summary>
    /// Starts a tag for a contact at <paramref name="anchor"/>, sitting with its top-left at <paramref name="position"/>
    /// while it has room. Both are in pixels. Add its lines with <see cref="WgTagLine"/>, the first being its name.
    /// </summary>
    private void WgBeginTag(Vector2 anchor, Vector2 position, int priority = 0)
    {
        _wgTags.Add(new WGRadarTagLayout.Tag { Anchor = anchor, Position = position, Priority = priority });
        _wgTagDraws.Add(new WgTagDraw { FirstLine = _wgTagLines.Count, FirstAbove = _wgTagAbove.Count });
    }

    /// <summary>Adds a line over the current tag's first line, each one higher than the last; shown only while the tag stands alone.</summary>
    private void WgTagAbove(DrawingHandleScreen handle, string text, float scale, Color color)
    {
        var size = handle.GetDimensions(Font, text, scale);
        _wgTagAbove.Add(new WgTagText(text, scale, color, size));
        var index = _wgTags.Count - 1;
        var tag = _wgTags[index];
        tag.Rise += size.Y;
        tag.Size.X = MathF.Max(tag.Size.X, size.X);
        _wgTags[index] = tag;
        var draw = _wgTagDraws[index];
        draw.AboveCount++;
        _wgTagDraws[index] = draw;
    }

    /// <summary>Adds a line under the current tag's others; the first line is the one a stack shows.</summary>
    private void WgTagLine(DrawingHandleScreen handle, string text, float scale, Color color)
    {
        var size = handle.GetDimensions(Font, text, scale);
        _wgTagLines.Add(new WgTagText(text, scale, color, size));
        var index = _wgTags.Count - 1;
        var tag = _wgTags[index];
        var draw = _wgTagDraws[index];
        if (draw.LineCount == 0)
        {
            draw.HeadTextWidth = size.X;
            tag.HeadSize += size;
        }

        draw.LineCount++;
        tag.Size = new Vector2(MathF.Max(tag.Size.X, size.X), tag.Size.Y + size.Y);
        _wgTags[index] = tag;
        _wgTagDraws[index] = draw;
    }

    /// <summary>Reserves <paramref name="width"/> pixels after the current tag's first line, drawn by <paramref name="draw"/> from that line's top-right.</summary>
    private void WgTagHeadExtra(float width, Action<DrawingHandleScreen, Vector2> draw)
    {
        var index = _wgTags.Count - 1;
        var tag = _wgTags[index];
        tag.HeadSize.X += width;
        tag.Size.X = MathF.Max(tag.Size.X, tag.HeadSize.X);
        _wgTags[index] = tag;
        var info = _wgTagDraws[index];
        info.DrawHeadExtra = draw;
        _wgTagDraws[index] = info;
    }

    /// <summary>Adds drawing the current tag does only while it stands alone, given its position.</summary>
    private void WgTagAlone(Action<DrawingHandleScreen, Vector2> draw)
    {
        var index = _wgTags.Count - 1;
        var info = _wgTagDraws[index];
        info.DrawAlone += draw;
        _wgTagDraws[index] = info;
    }

    /// <summary>Lays out and draws the tags collected this frame, over everything else on the scope.</summary>
    private void WgDrawTags(DrawingHandleScreen handle)
    {
        if (_wgTags.Count == 0)
            return;

        var moreScale = UIScale * 0.7f;
        _wgTagLayout.Gap = 3f * UIScale;
        _wgTagLayout.MoreHeight = handle.GetDimensions(Font, "+", moreScale).Y;
        _wgTagLayout.Solve(_wgTags, PixelSize);

        foreach (var group in _wgTagLayout.Groups)
        {
            if (!group.Stacked)
            {
                DrawTagAlone(handle, group.All[0]);
                continue;
            }

            var box = group.Box;
            var pad = 2f * UIScale;
            handle.DrawRect(new UIBox2(box.Left - pad, box.Bottom - pad, box.Right + pad, box.Top + pad), WgStackBacking);
            var y = box.Bottom;
            foreach (var index in group.Rows)
            {
                var tag = _wgTags[index];
                var draw = _wgTagDraws[index];
                var line = _wgTagLines[draw.FirstLine];
                var middle = y + tag.HeadSize.Y / 2f;
                var from = tag.Anchor.X > box.Right
                    ? new Vector2(box.Right + pad, middle)
                    : new Vector2(box.Left - pad, middle);
                if ((tag.Anchor - from).LengthSquared() > 16f * UIScale * UIScale)
                    handle.DrawLine(from, tag.Anchor, line.Color.WithAlpha(0.35f));

                var at = new Vector2(box.Left, y);
                handle.DrawString(Font, at, line.Text, line.Scale, line.Color);
                draw.DrawHeadExtra?.Invoke(handle, at + new Vector2(draw.HeadTextWidth, 0f));
                y += tag.HeadSize.Y;
            }

            if (group.Hidden > 0)
            {
                handle.DrawString(Font, new Vector2(box.Left, y), Loc.GetString("wg-radar-tags-more", ("count", group.Hidden)),
                    moreScale, WgStackMoreColor);
            }
        }

        _wgTags.Clear();
        _wgTagDraws.Clear();
        _wgTagLines.Clear();
        _wgTagAbove.Clear();
    }

    private void DrawTagAlone(DrawingHandleScreen handle, int index)
    {
        var tag = _wgTags[index];
        var draw = _wgTagDraws[index];
        var at = tag.Position;
        for (var i = 0; i < draw.LineCount; i++)
        {
            var line = _wgTagLines[draw.FirstLine + i];
            handle.DrawString(Font, at, line.Text, line.Scale, line.Color);
            if (i == 0)
                draw.DrawHeadExtra?.Invoke(handle, at + new Vector2(draw.HeadTextWidth, 0f));

            at.Y += line.Size.Y;
        }

        var above = tag.Position;
        for (var i = 0; i < draw.AboveCount; i++)
        {
            var line = _wgTagAbove[draw.FirstAbove + i];
            above.Y -= line.Size.Y;
            handle.DrawString(Font, above, line.Text, line.Scale, line.Color);
        }

        draw.DrawAlone?.Invoke(handle, tag.Position);
    }
}
