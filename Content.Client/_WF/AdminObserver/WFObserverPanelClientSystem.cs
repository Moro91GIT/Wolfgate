using Content.Client._WF.AdminObserver.UI;
using Content.Client.Administration.Managers;
using Content.Shared._WF.AdminObserver;

namespace Content.Client._WF.AdminObserver;

/// <summary>The observer panel's window and the requests it sends. The server saves each one as it arrives.</summary>
public sealed partial class WFObserverPanelClientSystem : EntitySystem
{
    [Dependency] private IClientAdminManager _admin = default!;

    private WFObserverPanelWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WFObserverPanelOpenEvent>(_ => Open());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _window?.Close();
        _window = null;
    }

    /// <summary>Opens the panel, or brings it to the front.</summary>
    public void Open()
    {
        if (_window is { IsOpen: true })
        {
            _window.MoveToFront();
            return;
        }

        _window = new WFObserverPanelWindow();
        _window.OpenCentered();
    }

    /// <summary>Opens the panel if it is closed and closes it if it is open, for the panel key.</summary>
    public void Toggle()
    {
        if (!_admin.CanCommand("observerpanel"))
            return;

        if (_window is { IsOpen: true })
            _window.Close();
        else
            Open();
    }

    public void SetMarker(WFStaffMarkerSettings marker) => RaiseNetworkEvent(new WFObserverSetMarkerEvent { Marker = marker });

    public void SavePreset(string? original, WFAvatarPreset preset) =>
        RaiseNetworkEvent(new WFObserverSavePresetEvent { Original = original, Preset = preset });

    public void DeletePreset(string name) => RaiseNetworkEvent(new WFObserverDeletePresetEvent { Name = name });

    public void SetDefault(string? name) => RaiseNetworkEvent(new WFObserverSetDefaultPresetEvent { Name = name });

    public void OpenTool(WFObserverTool tool) => RaiseNetworkEvent(new WFObserverToolRequestEvent { Tool = tool });

    public void Dress(string preset) => RaiseNetworkEvent(new WFAvatarDressRequestEvent { Preset = preset });

    public void SaveOutfit(string preset) => RaiseNetworkEvent(new WFAvatarSaveOutfitRequestEvent { Preset = preset });
}
