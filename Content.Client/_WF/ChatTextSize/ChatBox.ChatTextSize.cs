using System.Linq;
using Content.Client._WF.ChatTextSize;
using Content.Client.Stylesheets;
using Content.Shared._WF.ChatTextSize;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

// The chat log's text size and line spacing; see Content.Client/_WF/ChatTextSize.
public partial class ChatBox
{
    private const string WfTextSizeClass = "WFChatTextSize";

    /// <summary>The smallest chat text size a nonzero setting gives.</summary>
    public const int MinTextSize = 8;

    private int _wfTextSize;
    private int _wfLineSpacing;
    private Stylesheet? _wfBaseSheet;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        WfUpdateTextSize();
    }

    /// <summary>
    /// Gives the log its own copy of the theme with one more font rule when the player's size or spacing differs from
    /// the theme, and rebuilds it when the setting or the theme changes.
    /// </summary>
    private void WfUpdateTextSize()
    {
        var size = _cfg.GetCVar(ChatTextSizeCVars.Size);
        var spacing = _cfg.GetCVar(ChatTextSizeCVars.LineSpacing);
        var sheet = UserInterfaceManager.Stylesheet;
        if (size == _wfTextSize && spacing == _wfLineSpacing && sheet == _wfBaseSheet)
            return;

        _wfTextSize = size;
        _wfLineSpacing = spacing;
        _wfBaseSheet = sheet;

        Contents.Stylesheet = null;
        if (sheet == null || size <= 0 && spacing <= 0)
            return;

        Font font;
        if (size > 0)
        {
            font = IoCManager.Resolve<IResourceCache>().NotoStack(size: Math.Max(size, MinTextSize));
        }
        else
        {
            Contents.ForceRunStyleUpdate();
            font = Contents.TryGetStyleProperty<Font>("font", out var themed)
                ? themed
                : UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        if (spacing > 0)
            font = new WFSpacedFont(font, spacing);

        Contents.AddStyleClass(WfTextSizeClass);
        var rule = new StyleRule(
            new SelectorElement(typeof(OutputPanel), new[] { WfTextSizeClass }, null, null),
            new[] { new StyleProperty("font", font) });
        Contents.Stylesheet = new Stylesheet(sheet.Rules.Append(rule).ToList());
    }
}
