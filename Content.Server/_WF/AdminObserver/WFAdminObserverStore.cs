using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._WF.AdminObserver;

/// <summary>
/// Keeps each connected admin's <see cref="WFAdminObserverData"/> in memory: loaded when they join or gain the Admin
/// flag, sent to their client, and written back shortly after each change and when they leave or lose the flag.
/// </summary>
public sealed partial class WFAdminObserverStore : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    /// <summary>How long after the last change a document is written.</summary>
    public static readonly TimeSpan SaveDelay = TimeSpan.FromSeconds(2);

    /// <summary>Raised when an admin's document has come into memory.</summary>
    public event Action<ICommonSession>? Loaded;

    private readonly Dictionary<NetUserId, WFAdminObserverData> _data = new();
    private readonly Dictionary<NetUserId, CancellationTokenSource> _pending = new();
    private readonly HashSet<NetUserId> _loading = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WFAdminObserverRequestDataEvent>(OnRequest);
        _players.PlayerStatusChanged += OnStatusChanged;
        _admin.OnPermsChanged += OnPermsChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnStatusChanged;
        _admin.OnPermsChanged -= OnPermsChanged;

        foreach (var user in new List<NetUserId>(_pending.Keys))
        {
            _ = FlushAsync(user);
        }
    }

    /// <summary>Whether a player may use the admin observer: the Admin flag, as for <c>aghost</c>.</summary>
    public bool IsAllowed(ICommonSession session)
    {
        return _admin.HasAdminFlag(session, AdminFlags.Admin);
    }

    /// <summary>Whether a player's document is in memory.</summary>
    public bool IsLoaded(NetUserId user)
    {
        return _data.ContainsKey(user);
    }

    /// <summary>An admin's document, or null when they are not allowed or it has not loaded yet.</summary>
    public WFAdminObserverData? Get(ICommonSession session)
    {
        return IsAllowed(session) && _data.TryGetValue(session.UserId, out var data) ? data : null;
    }

    /// <summary>
    /// Changes an admin's document, then saves it and sends it to their client. False, with nothing changed, when
    /// they are not allowed, it has not loaded, or the result would be too large.
    /// </summary>
    public bool Change(ICommonSession session, Action<WFAdminObserverData> change)
    {
        return IsAllowed(session) && ChangeLoaded(session.UserId, change);
    }

    /// <summary>
    /// <see cref="Change"/> for a document that is in memory, without asking for the flag again. For what an
    /// admin's own observer saves as they leave it, when their session may already be closing.
    /// </summary>
    public bool ChangeLoaded(NetUserId user, Action<WFAdminObserverData> change)
    {
        if (!_data.TryGetValue(user, out var data))
            return false;

        WFAdminObserverSerializer.TrySerialize(data, out var before);
        change(data);
        Sanitize(data);

        if (!WFAdminObserverSerializer.TrySerialize(data, out _))
        {
            _data[user] = WFAdminObserverSerializer.Deserialize(before) ?? new WFAdminObserverData();
            return false;
        }

        ScheduleSave(user);
        if (_players.TryGetSessionById(user, out var session) && session.Status != SessionStatus.Disconnected)
            Send(session);

        return true;
    }

    /// <summary>Writes a document now if a save is waiting.</summary>
    public Task FlushAsync(NetUserId user)
    {
        if (!_pending.Remove(user, out var cancel))
            return Task.CompletedTask;

        cancel.Cancel();
        return SaveAsync(user);
    }

    /// <summary>Loads an admin's document if it is not in memory yet, then sends it to their client.</summary>
    public async Task EnsureLoadedAsync(ICommonSession session)
    {
        var user = session.UserId;
        if (!IsAllowed(session))
            return;

        if (_data.ContainsKey(user))
        {
            Send(session);
            return;
        }

        if (!_loading.Add(user))
            return;

        string? json = null;
        try
        {
            json = await _db.GetAdminObserverDataAsync(user.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Could not load the admin observer document of {session.Name}: {e}");
        }
        finally
        {
            _loading.Remove(user);
        }

        if (session.Status == SessionStatus.Disconnected || _data.ContainsKey(user))
            return;

        var data = json == null ? null : WFAdminObserverSerializer.Deserialize(json);
        if (json != null && data == null)
            Log.Warning($"The admin observer document of {session.Name} could not be read; starting a new one.");

        _data[user] = Sanitize(data ?? Stock());
        Send(session);
        Loaded?.Invoke(session);
    }

    /// <summary>
    /// A new admin's document: the plain ghost, their selected character in what it would spawn in, and that
    /// character as a Bluespace and a Subspace Technician (the AdminTech module's outfits).
    /// </summary>
    private WFAdminObserverData Stock()
    {
        var data = new WFAdminObserverData();
        data.Presets.Add(new WFAvatarPreset { Name = Loc.GetString("wf-observer-preset-stock-ghost") });
        data.Presets.Add(new WFAvatarPreset
        {
            Name = Loc.GetString("wf-observer-preset-stock-own"),
            Kind = WFAvatarKind.Character,
        });
        data.Presets.Add(new WFAvatarPreset
        {
            Name = Loc.GetString("wf-observer-preset-stock-bluespace"),
            Kind = WFAvatarKind.Character,
            Outfit = "WFAdminTechBluespace",
        });
        data.Presets.Add(new WFAvatarPreset
        {
            Name = Loc.GetString("wf-observer-preset-stock-subspace"),
            Kind = WFAvatarKind.Character,
            Outfit = "WFAdminTechSubspace",
        });
        return data;
    }

    private WFAdminObserverData Sanitize(WFAdminObserverData data)
    {
        return WFAdminObserverSerializer.Sanitize(
            data,
            id => _prototypes.TryIndex<EntityPrototype>(id, out var proto) && !proto.Abstract,
            id => _prototypes.HasIndex<StartingGearPrototype>(id));
    }

    private void Send(ICommonSession session)
    {
        RaiseNetworkEvent(new WFAdminObserverDataEvent { Data = Get(session) }, session);
    }

    private void ScheduleSave(NetUserId user)
    {
        if (_pending.Remove(user, out var old))
            old.Cancel();

        var cancel = new CancellationTokenSource();
        _pending[user] = cancel;
        Timer.Spawn(SaveDelay, () =>
        {
            if (_pending.TryGetValue(user, out var current) && current == cancel)
                _ = FlushAsync(user);
        }, cancel.Token);
    }

    private async Task SaveAsync(NetUserId user)
    {
        if (!_data.TryGetValue(user, out var data) || !WFAdminObserverSerializer.TrySerialize(data, out var json))
            return;

        try
        {
            await _db.SaveAdminObserverDataAsync(user.UserId, json);
        }
        catch (Exception e)
        {
            Log.Error($"Could not save the admin observer document of {user}: {e}");
        }
    }

    private void OnRequest(WFAdminObserverRequestDataEvent ev, EntitySessionEventArgs args)
    {
        _ = EnsureLoadedAsync(args.SenderSession);
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.InGame:
                _ = EnsureLoadedAsync(args.Session);
                break;
            case SessionStatus.Disconnected:
                // A frame later, so the observer the player is leaving can still save its kit.
                var user = args.Session.UserId;
                Timer.Spawn(TimeSpan.Zero, () => _ = UnloadAsync(user));
                break;
        }
    }

    private async Task UnloadAsync(NetUserId user)
    {
        await FlushAsync(user);
        if (!_players.TryGetSessionById(user, out var session) || session.Status == SessionStatus.Disconnected)
            _data.Remove(user);
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (IsAllowed(args.Player))
        {
            _ = EnsureLoadedAsync(args.Player);
            return;
        }

        RaiseNetworkEvent(new WFAdminObserverDataEvent(), args.Player);
        _ = DropAsync(args.Player);
    }

    /// <summary>Writes and forgets the document of a player who lost the flag.</summary>
    private async Task DropAsync(ICommonSession session)
    {
        await FlushAsync(session.UserId);
        if (!IsAllowed(session))
            _data.Remove(session.UserId);
    }
}
