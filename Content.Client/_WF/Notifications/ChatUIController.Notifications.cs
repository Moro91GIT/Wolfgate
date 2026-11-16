using Content.Client._WF.Notifications;
using Content.Shared._WF.Notifications;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private const string WfHighlightSound = "/Audio/_Goobstation/Effects/ding.ogg";
    private static readonly TimeSpan WfHighlightCooldown = TimeSpan.FromSeconds(2);

    private TimeSpan _wfNextHighlightAlert;

    /// <summary>Plays the highlight sound and flashes the window when highlighting changed someone else's message.</summary>
    private void WfOnHighlights(ChatMessage msg, string before)
    {
        if (msg.WrappedMessage == before || msg.HideChat)
            return;

        if (_player.LocalEntity is { } local && msg.SenderEntity == EntityManager.GetNetEntity(local))
            return;

        // A burst of matching messages alerts once.
        if (_timing.RealTime < _wfNextHighlightAlert)
            return;
        _wfNextHighlightAlert = _timing.RealTime + WfHighlightCooldown;

        if (_config.GetCVar(NotificationCVars.SoundHighlight))
            EntityManager.System<AudioSystem>().PlayGlobal(WfHighlightSound, Filter.Local(), false, AudioParams.Default.WithVolume(-4f));

        WFWindowFlash.Request(NotificationCVars.FlashHighlight);
    }
}
