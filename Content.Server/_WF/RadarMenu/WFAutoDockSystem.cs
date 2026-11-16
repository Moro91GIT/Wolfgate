using System.Numerics;
using Content.Server._Mono.NPC.HTN.Operators;
using Content.Server.NPC.HTN;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._Mono.Shuttles;
using Content.Shared._WF.RadarMenu;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._WF.RadarMenu;

/// <summary>
/// The helm side of the radar menu's "Dock at": lists the free docks on a grid that the ship can dock with, flies the
/// autopilot to the chosen one, and docks on arrival. If it cannot dock when it gets there, the autopilot stays off and
/// the pilot is told why.
/// </summary>
public sealed class WFAutoDockSystem : EntitySystem
{
    [Dependency] private DockingSystem _docking = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private ShuttleSystem _shuttles = default!;

    /// <summary>How far, in metres, a grid may be for its docks to be listed or flown to.</summary>
    public const float MaxRange = 6000f;

    /// <summary>
    /// How far, in metres, the ship may have stopped from its docked place for the docking clamps to pull it in. The
    /// helm's autopilot stops within 40 m of its target.
    /// </summary>
    public const float SnapRange = 50f;

    /// <summary>The most docks listed for one grid.</summary>
    public const int MaxOptions = 12;

    private static readonly TimeSpan RequestLife = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OptionsGap = TimeSpan.FromSeconds(0.5);

    private readonly Dictionary<EntityUid, TimeSpan> _nextOptions = new();

    public override void Initialize()
    {
        base.Initialize();

        // ShuttleConsoleSystem owns the open and close subscriptions for these consoles; only new message types here.
        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<WFAutoDockOptionsRequestMessage>(OnOptionsRequest);
            subs.Event<WFAutoDockRequestMessage>(OnDockRequest);
            subs.Event<WFAutopilotCancelMessage>(OnCancel);
        });

        SubscribeLocalEvent<WFAutoDockComponent, SteeringDoneEvent>(OnSteeringDone);
        // A new autopilot target from the map or a waypoint replaces the docking.
        SubscribeLocalEvent<WFAutoDockComponent, ShuttleConsoleAutopilotPositionMessage>(OnOtherAutopilot);
        SubscribeLocalEvent<Content.Shared.GameTicking.RoundRestartCleanupEvent>(_ => _nextOptions.Clear());
    }

    private void OnOptionsRequest(Entity<ShuttleConsoleComponent> ent, ref WFAutoDockOptionsRequestMessage args)
    {
        var now = _timing.CurTime;
        if (_nextOptions.TryGetValue(ent, out var next) && now < next)
            return;
        _nextOptions[ent] = now + OptionsGap;

        if (!TryGetEntity(args.Grid, out var grid) || !_players.TryGetSessionByEntity(args.Actor, out var session))
            return;

        var options = Options(ent, grid.Value);
        RaiseNetworkEvent(new WFAutoDockOptionsEvent(GetNetEntity(ent), args.Grid, options), session);
    }

    /// <summary>The free docks on a grid that the helm's ship can dock with now, nearest first.</summary>
    public List<WFDockOption> Options(EntityUid console, EntityUid grid)
    {
        var options = new List<WFDockOption>();
        if (!TryGetShip(console, grid, out var ship))
            return options;

        var shipPosition = _xform.GetWorldPosition(ship);
        var unnamed = 0;
        foreach (var dock in _docking.GetDocks(grid))
        {
            if (dock.Comp.Docked)
                continue;
            unnamed++;
            if (ConfigAt(ship, grid, dock) == null)
                continue;

            var name = dock.Comp.Name is { } label
                ? Loc.GetString(label)
                : Loc.GetString("wf-radar-menu-dock-unnamed", ("number", unnamed));
            var distance = Vector2.Distance(shipPosition, _xform.GetWorldPosition(dock));
            options.Add(new WFDockOption(GetNetEntity(dock), name, distance));
        }

        options.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        if (options.Count > MaxOptions)
            options.RemoveRange(MaxOptions, options.Count - MaxOptions);
        return options;
    }

    private void OnDockRequest(Entity<ShuttleConsoleComponent> ent, ref WFAutoDockRequestMessage args)
    {
        if (!TryGetEntity(args.Grid, out var grid))
            return;

        EntityUid? dock = args.Dock is { } netDock && TryGetEntity(netDock, out var found) ? found : null;
        if (args.Dock != null && dock == null)
            return;

        TryStart(ent, grid.Value, dock, args.Actor);
    }

    /// <summary>
    /// Sends the helm's autopilot to dock at a grid: at one dock, or at the nearest free one that fits. False, with the
    /// pilot told why, when there is no such dock.
    /// </summary>
    public bool TryStart(Entity<ShuttleConsoleComponent> console, EntityUid grid, EntityUid? dock, EntityUid? pilot)
    {
        if (!TryComp<HTNComponent>(console, out var htn))
            return false;

        if (!TryGetShip(console, grid, out var ship))
        {
            Tell(console, pilot, "wf-radar-menu-dock-out-of-reach");
            return false;
        }

        if (_docking.GetDocks(ship).Exists(own => own.Comp.Docked))
        {
            Tell(console, pilot, "wf-radar-menu-dock-undock-first");
            return false;
        }

        var config = Plan(ship, grid, dock);
        if (config == null)
        {
            Tell(console, pilot, "wf-radar-menu-dock-none");
            return false;
        }

        // Aim for the ship's docked place, turned as it will sit there.
        var heading = config.Angle + _xform.GetWorldRotation(config.Coordinates.EntityId);
        htn.Blackboard.SetValue(console.Comp.AutopilotTargetKey, config.Coordinates);
        htn.Blackboard.SetValue(console.Comp.AutopilotRotationKey, heading + MathF.PI);

        var request = EnsureComp<WFAutoDockComponent>(console);
        request.TargetGrid = grid;
        request.Dock = dock ?? config.Docks[0].DockBUid;
        request.Pilot = pilot;
        request.Expires = _timing.CurTime + RequestLife;

        Tell(console, pilot, "wf-radar-menu-dock-started", ("grid", Name(grid)));
        return true;
    }

    private void OnSteeringDone(Entity<WFAutoDockComponent> ent, ref SteeringDoneEvent args)
    {
        var request = ent.Comp;
        RemCompDeferred<WFAutoDockComponent>(ent);
        if (_timing.CurTime > request.Expires)
            return;

        if (!TryFinish(ent, request, out var reason))
        {
            StopAutopilot(ent);
            Tell(ent, request.Pilot, reason);
        }
    }

    /// <summary>Docks a ship that has arrived. False with the reason when it cannot.</summary>
    public bool TryFinish(EntityUid console, WFAutoDockComponent request, out string reason)
    {
        reason = "wf-radar-menu-dock-failed-gone";
        if (TerminatingOrDeleted(request.TargetGrid) || !TryGetShip(console, request.TargetGrid, out var ship))
            return false;

        // The dock asked for if it is still free, else any other that fits.
        var config = Plan(ship, request.TargetGrid, request.Dock) ?? Plan(ship, request.TargetGrid, null);
        if (config == null)
        {
            reason = "wf-radar-menu-dock-failed-taken";
            return false;
        }

        var docked = _xform.ToMapCoordinates(config.Coordinates);
        if (Vector2.Distance(_xform.GetWorldPosition(ship), docked.Position) > SnapRange)
        {
            reason = "wf-radar-menu-dock-failed-short";
            return false;
        }

        _physics.SetLinearVelocity(ship, Vector2.Zero);
        _physics.SetAngularVelocity(ship, 0f);
        _shuttles.FTLDock((ship, Transform(ship)), config);
        Tell(console, request.Pilot, "wf-radar-menu-dock-done", ("grid", Name(request.TargetGrid)));
        reason = string.Empty;
        return true;
    }

    private void OnOtherAutopilot(Entity<WFAutoDockComponent> ent, ref ShuttleConsoleAutopilotPositionMessage args)
    {
        RemCompDeferred<WFAutoDockComponent>(ent);
    }

    private void OnCancel(Entity<ShuttleConsoleComponent> ent, ref WFAutopilotCancelMessage args)
    {
        RemCompDeferred<WFAutoDockComponent>(ent);
        StopAutopilot(ent);
        Tell(ent, args.Actor, "wf-radar-menu-autopilot-cancelled");
    }

    /// <summary>Clears the helm's autopilot target, which ends the flight on the planner's next update.</summary>
    public void StopAutopilot(EntityUid console)
    {
        if (!TryComp<HTNComponent>(console, out var htn) || !TryComp<ShuttleConsoleComponent>(console, out var helm))
            return;

        htn.Blackboard.Remove<EntityCoordinates>(helm.AutopilotTargetKey);
        htn.Blackboard.Remove<Angle>(helm.AutopilotRotationKey);
    }

    /// <summary>The docking config for one dock of a grid, or for any of its free docks when none is named.</summary>
    private DockingConfig? Plan(EntityUid ship, EntityUid grid, EntityUid? dock)
    {
        if (dock == null)
            return _docking.GetDockingConfig(ship, grid);

        if (!TryComp<DockingComponent>(dock, out var gridDock) || gridDock.Docked || Transform(dock.Value).GridUid != grid)
            return null;

        return ConfigAt(ship, grid, (dock.Value, gridDock));
    }

    /// <summary>A config docking any of the ship's free docks to this one, or null when none fits.</summary>
    private DockingConfig? ConfigAt(EntityUid ship, EntityUid grid, Entity<DockingComponent> gridDock)
    {
        foreach (var own in _docking.GetDocks(ship))
        {
            if (own.Comp.Docked)
                continue;
            if (_docking.GetDockingConfig(ship, grid, own, own.Comp, gridDock, gridDock.Comp) is { } config)
                return config;
        }

        return null;
    }

    /// <summary>The ship a helm flies, when it can reach the grid: same map, in range, not in FTL, and not the grid itself.</summary>
    private bool TryGetShip(EntityUid console, EntityUid grid, out EntityUid ship)
    {
        ship = default;
        if (Transform(console).GridUid is not { } own || own == grid || !HasComp<ShuttleComponent>(own)
            || HasComp<FTLComponent>(own) || !HasComp<MapGridComponent>(grid)
            || Transform(own).MapID != Transform(grid).MapID
            || Vector2.Distance(_xform.GetWorldPosition(own), _xform.GetWorldPosition(grid)) > MaxRange)
            return false;

        ship = own;
        return true;
    }

    private string Name(EntityUid grid) => MetaData(grid).EntityName;

    private void Tell(EntityUid console, EntityUid? pilot, string key, params (string, object)[] args)
    {
        var message = Loc.GetString(key, args);
        if (pilot is { } user && Exists(user))
            _popup.PopupEntity(message, console, user, PopupType.Medium);
        else
            _popup.PopupEntity(message, console, PopupType.Medium);
    }
}
