using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Shared._WF.Wilderness;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._WF.Wilderness;

/// <summary>
/// The guidebook's <c>&lt;WFWildernessRule/&gt;</c>: who may fight in a Wilderness, and how the mark works, from the
/// server's settings, so the rule says what the server does.
/// </summary>
[UsedImplicitly]
public sealed class WFWildernessRule : RichTextLabel, IDocumentTag
{
    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        var cfg = IoCManager.Resolve<IConfigurationManager>();
        var message = new FormattedMessage();
        message.AddMarkupPermissive(Loc.GetString("wf-wilderness-rule-who",
            ("open", cfg.GetCVar(WildernessCVars.OpenToAll) ? "yes" : "no")));
        message.PushNewline();
        message.AddMarkupPermissive(Loc.GetString("wf-wilderness-rule-mark",
            ("seconds", cfg.GetCVar(WildernessCVars.DwellSeconds)),
            ("minutes", cfg.GetCVar(WildernessCVars.SkullMinutes)),
            ("onfoot", cfg.GetCVar(WildernessCVars.SkullOnFoot) ? "yes" : "no")));
        SetMessage(message);
        control = this;
        return true;
    }
}
