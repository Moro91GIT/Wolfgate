using System.Linq;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Cargo;
using Content.Shared.Communications;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Solar;
using Content.Shared.StationRecords;
using Content.Shared.UserInterface;
using Robust.Shared.Player;

namespace Content.Server._WF.AdminObserver;

/// <summary>
/// What the observer panel asks for. Every choice is written to the admin's saved document as it is made, and a
/// change to the staff marker shows at once on an admin who is marked. The store refuses anyone without the Admin
/// flag, so nothing here needs to check it again.
/// </summary>
public sealed partial class WFObserverPanelSystem : EntitySystem
{
    [Dependency] private IntrinsicUISystem _intrinsic = default!;
    [Dependency] private WFAdminObserverKitSystem _kit = default!;
    [Dependency] private WFAdminObserverStore _store = default!;
    [Dependency] private WFManifestSystem _manifest = default!;
    [Dependency] private WFStaffMarkerSystem _marker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFAdminObserverComponent, WFOpenObserverPanelEvent>(OnAction);
        SubscribeNetworkEvent<WFObserverSetMarkerEvent>((ev, args) => SetMarker(args.SenderSession, ev.Marker));
        SubscribeNetworkEvent<WFObserverSavePresetEvent>((ev, args) => SavePreset(args.SenderSession, ev.Original, ev.Preset));
        SubscribeNetworkEvent<WFObserverDeletePresetEvent>((ev, args) => DeletePreset(args.SenderSession, ev.Name));
        SubscribeNetworkEvent<WFObserverSetDefaultPresetEvent>((ev, args) => SetDefault(args.SenderSession, ev.Name));
        SubscribeNetworkEvent<WFObserverToolRequestEvent>((ev, args) => OpenTool(args.SenderSession, ev.Tool));
    }


    private void OnAction(Entity<WFAdminObserverComponent> ent, ref WFOpenObserverPanelEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        Open(actor.PlayerSession);
    }

    /// <summary>Opens the observer panel on an admin's client. Nothing happens for anyone else.</summary>
    public bool Open(ICommonSession player)
    {
        if (!_store.IsAllowed(player))
            return false;

        RaiseNetworkEvent(new WFObserverPanelOpenEvent(), player);
        return true;
    }

    /// <summary>Saves an admin's staff marker choices, and shows them at once if the admin is marked right now.</summary>
    public bool SetMarker(ICommonSession player, WFStaffMarkerSettings marker)
    {
        if (!_store.Change(player, data => data.Marker = marker) || _store.Get(player) is not { } saved)
            return false;

        if (player.AttachedEntity is { } uid && HasComp<WFStaffMarkerComponent>(uid))
        {
            var kind = TryComp<WFManifestedComponent>(uid, out var manifested) ? manifested.Kind : WFAvatarKind.Ghost;
            _marker.Mark(uid, player, saved.Marker, kind);
        }

        return true;
    }

    /// <summary>
    /// Adds an avatar preset, or replaces the one called <paramref name="original"/>. A preset is known by the
    /// name players will see where it has one and that name is free; a new one with no name gets a numbered one.
    /// </summary>
    public bool SavePreset(ICommonSession player, string? original, WFAvatarPreset preset)
    {
        if (_store.Get(player) is not { } data)
            return false;

        if (preset.Kind is WFAvatarKind.Object or WFAvatarKind.Mob && !_manifest.IsAvatarPrototype(preset.Prototype))
            return false;

        var existing = original == null ? null : data.Presets.FirstOrDefault(p => p.Name == original);
        if (original != null && existing == null)
            return false;

        bool Taken(string name) => data.Presets.Any(p => p != existing && p.Name == name);

        var shown = preset.DisplayName.Trim();
        if (shown.Length > WFAdminObserverData.MaxNameLength)
            shown = shown[..WFAdminObserverData.MaxNameLength];

        if (shown.Length > 0 && !Taken(shown))
        {
            preset.Name = shown;
        }
        else if (existing != null)
        {
            preset.Name = existing.Name;
        }
        else
        {
            var stem = Loc.GetString("wf-observer-preset-new");
            preset.Name = stem;
            for (var number = 2; Taken(preset.Name); number++)
            {
                preset.Name = $"{stem} {number}";
            }
        }

        return _store.Change(player, saved =>
        {
            var at = existing == null ? -1 : saved.Presets.FindIndex(p => p.Name == existing.Name);
            if (at < 0)
                saved.Presets.Add(preset);
            else
                saved.Presets[at] = preset;

            if (existing != null && saved.DefaultPreset == existing.Name)
                saved.DefaultPreset = preset.Name;
        });
    }

    /// <summary>Deletes one of an admin's avatar presets.</summary>
    public bool DeletePreset(ICommonSession player, string name)
    {
        if (_store.Get(player)?.Presets.Any(p => p.Name == name) != true)
            return false;

        return _store.Change(player, data =>
        {
            data.Presets.RemoveAll(p => p.Name == name);
            if (data.DefaultPreset == name)
                data.DefaultPreset = null;
        });
    }

    /// <summary>Makes a preset the one a plain manifest uses; null goes back to the ghost.</summary>
    public bool SetDefault(ICommonSession player, string? name)
    {
        if (name != null && _store.Get(player)?.Presets.Any(p => p.Name == name) != true)
            return false;

        return _store.Change(player, data => data.DefaultPreset = name);
    }

    /// <summary>Opens one of the consoles an admin observer carries, for the admin in it.</summary>
    public bool OpenTool(ICommonSession player, WFObserverTool tool)
    {
        if (!_store.IsAllowed(player) || !_kit.TryGetObserver(player, out var observer))
            return false;

        Enum? key = tool switch
        {
            WFObserverTool.Solar => SolarControlConsoleUiKey.Key,
            WFObserverTool.Communications => CommunicationsConsoleUiKey.Key,
            WFObserverTool.Radar => RadarConsoleUiKey.Key,
            WFObserverTool.Cargo => CargoConsoleUiKey.Orders,
            WFObserverTool.CrewMonitor => CrewMonitoringUIKey.Key,
            WFObserverTool.Records => GeneralStationRecordConsoleKey.Key,
            _ => null,
        };

        return key != null && _intrinsic.InteractUI(observer, key);
    }
}
