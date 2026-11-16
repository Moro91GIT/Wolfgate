using Content.Shared._WF.AdminObserver;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    /// <summary>Whether the local player is a manifested admin observer, which speaks as the living do.</summary>
    private bool WFManifested => _player.LocalEntity is { } local && _ent.HasComponent<WFManifestedComponent>(local);

    /// <summary>Works out again which channels the player may use, as when their observer manifests or hides.</summary>
    public void WFRefreshChannelPermissions()
    {
        UpdateChannelPermissions();
    }
}
