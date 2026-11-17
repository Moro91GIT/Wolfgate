using System.Linq;
using System.Numerics;
using Content.Server._NF.Bank;
using Content.Server._WF.Bench;
using Content.Server._WF.Companies;
using Content.Server._WF.Encounters.Components;
using Content.Server._WF.Encounters.Systems;
using Content.Server._WF.NpcCrew;
using Content.Server._WF.ShipShields;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._WF.Encounters;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chat;
using Content.Shared._WF.JudgeLine;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._WF.JudgeLine;

/// <summary>
/// The Judge Line: a vagrant in the Colossus sector pays to call a judge ship to an emergency. Calls queue for a free
/// responder, which drops out of FTL near the caller after the dispatch delay, checks the emergency is real and deals
/// with it (see the Plan partial), then leaves.
/// </summary>
public sealed partial class WFJudgeLineSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WFBenchSystem _bench = default!;
    [Dependency] private WFCompanySystem _companies = default!;
    [Dependency] private WFEncounterSystem _encounters = default!;
    [Dependency] private WFEncounterSchedulerSystem _scheduler = default!;

    /// <summary>The encounter the responder flies as.</summary>
    public static readonly ProtoId<WFEncounterPrototype> Responder = "WFEncounterJudgeResponder";

    /// <summary>The responder's ship key in its encounter.</summary>
    public const string ShipKey = "judge";

    public static readonly EntProtoId Cartridge = "WFJudgeLineCartridge";

    /// <summary>Hoaxes after which a caller is refused for the rest of the round.</summary>
    public const int StrikeLimit = 3;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(20);
    private const int SpawnTries = 3;
    private const float SpawnClearance = 150f;

    private readonly List<WFJudgeLineCall> _calls = new();
    private readonly Dictionary<NetUserId, int> _strikes = new();
    private readonly Dictionary<NetUserId, TimeSpan> _cooldowns = new();
    private readonly Dictionary<int, int> _spawnFailures = new();
    private int _nextId = 1;
    private TimeSpan _next;

    public IReadOnlyList<WFJudgeLineCall> Calls => _calls;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<WFCrewHullHitEvent>(OnHullHit);
        SubscribeLocalEvent<WFShipShieldAttackedEvent>(OnShieldHit);
        SubscribeLocalEvent<WFJudgeLineCallerComponent, DamageChangedEvent>(OnCallerDamaged);
        InitializeUi();
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _calls.Clear();
        _strikes.Clear();
        _cooldowns.Clear();
        _spawnFailures.Clear();
        _hits.Clear();
        _nextId = 1;
    }

    /// <summary>Installs the Judge Line app on a vagrant's PDA.</summary>
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId != SharedGameTicker.FallbackOverflowJob.Id || _companies.CompanyIdOf(ev.Player) != null
            || !_inventory.TryGetSlotEntity(ev.Mob, "id", out var pda) || !HasComp<CartridgeLoaderComponent>(pda))
            return;

        _cartridgeLoader.InstallProgram(pda.Value, Cartridge, deinstallable: false);
    }

    private void OnCallerDamaged(Entity<WFJudgeLineCallerComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var now = _timing.CurTime;
        ent.Comp.LastHurt = now;
        if (args.Origin is { } origin && origin != ent.Owner)
            ent.Comp.Assailants[origin] = now;
    }

    /// <summary>Whether someone is a vagrant: in the overflow job and in no company.</summary>
    public bool IsVagrant(EntityUid user)
    {
        return _mind.TryGetMind(user, out var mindId, out _)
            && _jobs.MindTryGetJobId(mindId, out var job)
            && job == SharedGameTicker.FallbackOverflowJob
            && TryComp<ActorComponent>(user, out var actor)
            && _companies.CompanyIdOf(actor.PlayerSession) == null;
    }

    /// <summary>The open call someone placed, if any.</summary>
    public WFJudgeLineCall? CallOf(EntityUid caller)
    {
        return _calls.FirstOrDefault(call => call.Caller == caller && call.Phase != WFJudgeLinePhase.Leaving);
    }

    /// <summary>Why someone can't call the judges now, or null if they can.</summary>
    public string? Refusal(EntityUid caller)
    {
        if (!_config.GetCVar(JudgeLineCVars.Enabled))
            return Loc.GetString("wf-judgeline-refuse-offline");
        if (!IsVagrant(caller))
            return Loc.GetString("wf-judgeline-refuse-not-vagrant");
        if (CallOf(caller) != null)
            return Loc.GetString("wf-judgeline-refuse-open");

        var xform = Transform(caller);
        if (xform.MapID != (SectorOverride ?? _ticker.DefaultMap))
            return Loc.GetString("wf-judgeline-refuse-sector");
        if (xform.GridUid is { } grid && IsStation(grid))
            return Loc.GetString("wf-judgeline-refuse-station");

        if (TryComp<ActorComponent>(caller, out var actor))
        {
            var user = actor.PlayerSession.UserId;
            if (_strikes.GetValueOrDefault(user) >= StrikeLimit)
                return Loc.GetString("wf-judgeline-refuse-barred");
            if (_cooldowns.TryGetValue(user, out var until) && _timing.CurTime < until)
                return Loc.GetString("wf-judgeline-refuse-cooldown", ("minutes", (int) Math.Ceiling((until - _timing.CurTime).TotalMinutes)));
            if (_bench.Owed(user) > 0)
                return Loc.GetString("wf-judgeline-refuse-warrant");
        }

        var fee = _config.GetCVar(JudgeLineCVars.Fee);
        if (!_bank.TryGetBalance(caller, out var balance) || balance < fee)
            return Loc.GetString("wf-judgeline-refuse-funds", ("fee", fee));

        return null;
    }

    /// <summary>Takes the fee and places a call, or says why not.</summary>
    public bool TryCall(EntityUid caller, WFJudgeLineKind kind, out string? refusal)
    {
        refusal = Refusal(caller);
        if (refusal != null)
            return false;

        var fee = _config.GetCVar(JudgeLineCVars.Fee);
        if (!_bank.TryBankWithdraw(caller, fee))
        {
            refusal = Loc.GetString("wf-judgeline-refuse-funds", ("fee", fee));
            return false;
        }

        Place(caller, kind, fee);
        return true;
    }

    /// <summary>Places a call without any check or charge: the admin command and tests use it directly.</summary>
    public WFJudgeLineCall Place(EntityUid caller, WFJudgeLineKind kind, int fee = 0)
    {
        var call = new WFJudgeLineCall
        {
            Id = _nextId++,
            Caller = caller,
            User = CompOrNull<ActorComponent>(caller)?.PlayerSession.UserId,
            Chosen = kind,
            Kind = kind,
            Fee = fee,
            Placed = _timing.CurTime,
            Position = _transform.GetMapCoordinates(caller),
        };
        call.Grid = OwnShip(caller);
        call.LastTrouble = call.Placed;
        EnsureComp<WFJudgeLineCallerComponent>(caller);
        call.RealAtCall = Assess(call).Count > 0;
        _calls.Add(call);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(caller):player} called the Judge Line: {kind}, fee {fee}, real at call {call.RealAtCall}");
        Notify(call, Loc.GetString("wf-judgeline-status-received", ("kind", KindName(kind))));
        Dispatch();
        return call;
    }

    /// <summary>The map that counts as the Colossus sector instead of the round's default map; tests set it.</summary>
    public MapId? SectorOverride { get; set; }

    /// <summary>Clears a player's hoax strikes and cooldown.</summary>
    public void Pardon(NetUserId user)
    {
        _strikes.Remove(user);
        _cooldowns.Remove(user);
    }

    /// <summary>Cancels a caller's own call. The fee is refunded only while it is still queued.</summary>
    public bool Cancel(EntityUid caller)
    {
        return CallOf(caller) is { } call && Cancel(call);
    }

    /// <summary>Cancels one call; a queued one is refunded.</summary>
    public bool Cancel(WFJudgeLineCall call)
    {
        if (call.Phase == WFJudgeLinePhase.Leaving || !_calls.Contains(call))
            return false;

        Close(call, Loc.GetString("wf-judgeline-status-cancelled"), refund: call.Phase == WFJudgeLinePhase.Queued);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _next)
            return;

        _next = _timing.CurTime + Interval;
        PruneHits();
        foreach (var call in _calls.ToList())
        {
            Tick(call);
        }

        Dispatch();
        RefreshOpen();
    }

    /// <summary>Gives queued calls a responder while fewer than the limit are flying.</summary>
    private void Dispatch()
    {
        var limit = _config.GetCVar(JudgeLineCVars.Responders);
        var flying = _calls.Count(call => call.Phase is WFJudgeLinePhase.Dispatched or WFJudgeLinePhase.OnScene);
        foreach (var call in _calls.Where(c => c.Phase == WFJudgeLinePhase.Queued).OrderBy(c => c.Placed).ToList())
        {
            if (flying >= limit)
                break;

            call.Phase = WFJudgeLinePhase.Dispatched;
            call.DueAt = _timing.CurTime + TimeSpan.FromSeconds(_config.GetCVar(JudgeLineCVars.DispatchDelay));
            flying++;
            Notify(call, Loc.GetString("wf-judgeline-status-dispatched",
                ("seconds", (int) Math.Ceiling((call.DueAt - _timing.CurTime).TotalSeconds))));
        }
    }

    private void Tick(WFJudgeLineCall call)
    {
        if (call.Phase == WFJudgeLinePhase.Leaving)
        {
            if (call.Encounter is not { } encounter || TerminatingOrDeleted(encounter))
                _calls.Remove(call);
            return;
        }

        if (TerminatingOrDeleted(call.Caller))
        {
            Close(call, null, refund: call.Phase == WFJudgeLinePhase.Queued);
            return;
        }

        var here = _transform.GetMapCoordinates(call.Caller);
        if (here.MapId != call.Position.MapId)
        {
            Close(call, Loc.GetString("wf-judgeline-status-left"), refund: false);
            return;
        }

        call.Position = here;
        switch (call.Phase)
        {
            case WFJudgeLinePhase.Dispatched when _timing.CurTime >= call.DueAt:
                if (TrySpawnResponder(call))
                    break;

                var failures = _spawnFailures[call.Id] = _spawnFailures.GetValueOrDefault(call.Id) + 1;
                if (failures >= SpawnTries)
                    Close(call, Loc.GetString("wf-judgeline-status-unavailable"), refund: true);
                else
                    call.DueAt = _timing.CurTime + RetryDelay;
                break;
            case WFJudgeLinePhase.OnScene:
                if (Ship(call) is not { } ship)
                {
                    // Lost before it got there: the judges owe the fee back. Lost on scene, they did their part.
                    Close(call, Loc.GetString("wf-judgeline-status-lost"), refund: call.Arrived == null);
                    break;
                }

                Run(call, ship);
                break;
        }
    }

    /// <summary>Drops the responder out of FTL near the caller, on a clear bearing.</summary>
    private bool TrySpawnResponder(WFJudgeLineCall call)
    {
        if (!_prototypes.TryIndex(Responder, out var prototype))
            return false;

        var distance = _config.GetCVar(JudgeLineCVars.ArrivalDistance);
        var start = _random.NextFloat(MathF.Tau);
        for (var step = 0; step < 8; step++)
        {
            var angle = start + MathF.Tau * step / 8;
            var point = call.Position.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            if (!_scheduler.IsClearSpace(call.Position.MapId, point, SpawnClearance, 0f))
                continue;

            // The caller is the spawner, so a raging battle doesn't keep the judges out of the very fight they were called to.
            if (!_encounters.TrySpawn(prototype, new MapCoordinates(point, call.Position.MapId), out var encounter,
                    spawner: call.Caller, pinned: true, offBudget: true))
                return false;

            call.Encounter = encounter;
            call.Phase = WFJudgeLinePhase.OnScene;
            if (Ship(call) is not { } ship)
                return false;

            Begin(call, ship);
            Notify(call, Loc.GetString("wf-judgeline-status-inbound"));
            Say(call, ship, Loc.GetString("wf-judgeline-radio-inbound"));
            return true;
        }

        return false;
    }

    /// <summary>The responder ship of a call, while it stands.</summary>
    public WFEncounterShipState? Ship(WFJudgeLineCall call)
    {
        if (call.Encounter is not { } uid || !TryComp<WFEncounterComponent>(uid, out var encounter)
            || !encounter.Ships.TryGetValue(ShipKey, out var ship) || TerminatingOrDeleted(ship.Grid))
            return null;

        return ship;
    }

    /// <summary>
    /// Ends a call: the responder, if any, stands its men down and leaves; the fee is refunded if asked. A caller who
    /// placed a real call waits the plain cooldown before the next.
    /// </summary>
    private void Close(WFJudgeLineCall call, string? message, bool refund, TimeSpan? cooldown = null)
    {
        if (message != null)
            Notify(call, message);

        if (refund && call.Fee > 0 && !TerminatingOrDeleted(call.Caller))
            _bank.TryBankDeposit(call.Caller, call.Fee, tax: false);

        if (call.User is { } user)
        {
            var until = _timing.CurTime + (cooldown ?? TimeSpan.FromMinutes(_config.GetCVar(JudgeLineCVars.Cooldown)));
            if (!_cooldowns.TryGetValue(user, out var held) || held < until)
                _cooldowns[user] = until;
        }

        RemComp<WFJudgeLineCallerComponent>(call.Caller);
        _spawnFailures.Remove(call.Id);
        StandDown(call);
        call.Phase = WFJudgeLinePhase.Leaving;
        if (call.Encounter == null)
            _calls.Remove(call);
    }

    /// <summary>Tells the caller how the call stands, in chat and in the app.</summary>
    private void Notify(WFJudgeLineCall call, string message)
    {
        call.Status = message;
        if (!TryComp<ActorComponent>(call.Caller, out var actor))
            return;

        var wrapped = Loc.GetString("wf-judgeline-chat", ("message", FormattedMessage.EscapeText(message)));
        _chat.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, actor.PlayerSession.Channel);
    }

    public string KindName(WFJudgeLineKind kind)
    {
        return Loc.GetString($"wf-judgeline-kind-{kind.ToString().ToLowerInvariant()}");
    }

    /// <summary>The ship someone stands on that could be theirs: no station and no encounter's hull.</summary>
    private EntityUid? OwnShip(EntityUid uid)
    {
        if (Transform(uid).GridUid is not { } grid || IsStation(grid)
            || HasComp<WFEncounterGridComponent>(grid))
            return null;

        return grid;
    }

    /// <summary>A real station: a station member that is no player-bought shuttle.</summary>
    private bool IsStation(EntityUid grid)
    {
        return HasComp<StationMemberComponent>(grid) && !HasComp<ShuttleDeedComponent>(grid);
    }
}
