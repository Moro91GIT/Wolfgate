using Robust.Shared.Configuration;

namespace Content.Shared._WF.ChatTextSize;

/// <summary>How large the chat log's text is and how far apart its lines sit.</summary>
[CVarDefs]
public sealed class ChatTextSizeCVars
{
    /// <summary>The chat log's font size. 0 keeps the UI theme's font.</summary>
    public static readonly CVarDef<int> Size =
        CVarDef.Create("wf.chat_text_size", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>Extra pixels between lines of the chat log.</summary>
    public static readonly CVarDef<int> LineSpacing =
        CVarDef.Create("wf.chat_line_spacing", 0, CVar.ARCHIVE | CVar.CLIENTONLY);
}
