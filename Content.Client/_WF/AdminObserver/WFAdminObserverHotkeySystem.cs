using Content.Client.Administration.Managers;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;

namespace Content.Client._WF.AdminObserver;

/// <summary>Runs <c>aghost</c> from a key: into the admin observer, and back to the body it left.</summary>
public sealed partial class WFAdminObserverHotkeySystem : EntitySystem
{
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private WFManifestClientSystem _manifest = default!;
    [Dependency] private WFObserverPanelClientSystem _panel = default!;

    private const string AghostCommand = "aghost";
    private const string ManifestCommand = "manifest";
    private const string UnmanifestCommand = "unmanifest";

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(WFAdminObserverKeys.WFToggleAghost, InputCmdHandler.FromDelegate(_ => ToggleAghost(), handle: true,
                outsidePrediction: true))
            .Bind(WFAdminObserverKeys.WFToggleManifest, InputCmdHandler.FromDelegate(_ => ToggleManifest(), handle: true,
                outsidePrediction: true))
            .Bind(WFAdminObserverKeys.WFOpenObserverPanel, InputCmdHandler.FromDelegate(_ => _panel.Toggle(), handle: true,
                outsidePrediction: true))
            .Register<WFAdminObserverHotkeySystem>();
    }

    /// <summary>Shows the admin observer to players, or hides it again.</summary>
    public void ToggleManifest()
    {
        var command = _manifest.LocalManifested() ? UnmanifestCommand : ManifestCommand;
        if (_admin.CanCommand(command))
            _console.ExecuteCommand(command);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<WFAdminObserverHotkeySystem>();
    }

    /// <summary>Runs <c>aghost</c> for an admin, unless it would only swap one bodiless observer for another.</summary>
    public void ToggleAghost()
    {
        if (!_admin.CanCommand(AghostCommand))
            return;

        if (HasNoBodyToReturnTo())
        {
            _popup.PopupCursor(Loc.GetString("wf-observer-hotkey-no-body"));
            return;
        }

        _console.ExecuteCommand(AghostCommand);
    }

    /// <summary>Whether the local player is an admin observer that is not visiting from a body.</summary>
    public bool HasNoBodyToReturnTo()
    {
        return _player.LocalEntity is { } entity
               && TryComp<GhostComponent>(entity, out var ghost)
               && ghost.CanGhostInteract
               && !ghost.CanReturnToBody;
    }
}
