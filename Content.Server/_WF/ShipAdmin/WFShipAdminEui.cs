using System.IO;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._WF.ShipAdmin;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Server._WF.ShipAdmin;

/// <summary>Server half of the admin ship panel: one admin's filter, selection and last result.</summary>
public sealed partial class WFShipAdminEui : BaseEui
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IRobustSerializer _serializer = default!;

    private readonly WFShipAdminSystem _ships;

    private bool _sendList = true;
    private byte[] _sentDetail = Array.Empty<byte>();
    private string _result = string.Empty;
    private bool _resultError;
    private int _resultSeq;
    private EntityUid? _scanned;
    private int? _value;

    public WFShipAdminFilter Filter { get; private set; }
    public EntityUid? Selected { get; private set; }

    public WFShipAdminEui(EntityUid? selected = null)
    {
        IoCManager.InjectDependencies(this);
        _ships = _systems.GetEntitySystem<WFShipAdminSystem>();
        Selected = selected;
    }

    public override void Opened()
    {
        _ships.PanelOpened(this);
        _admin.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        _admin.OnPermsChanged -= OnPermsChanged;
        _ships.PanelClosed(this);
    }

    public override EuiStateBase GetNewState()
    {
        var state = new WFShipAdminEuiState
        {
            Filter = Filter,
            ViewerFlags = _ships.Flags(Player),
            Result = _result,
            ResultError = _resultError,
            ResultSeq = _resultSeq,
        };

        if (_sendList)
        {
            state.Ships = _ships.BuildList(Filter, out state.Truncated);
            _sendList = false;
        }

        state.Selected = Detail();
        _sentDetail = Pack(state.Selected);
        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_admin.HasAdminFlag(Player, WFShipAdminPermissions.Base))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case WFShipAdminSelectMessage select:
                Select(_entMan.TryGetEntity(select.Grid, out var picked) ? picked : null);
                break;
            case WFShipAdminListMessage list when Enum.IsDefined(list.Filter):
                Filter = list.Filter;
                RefreshList();
                break;
            case WFShipAdminActionMessage action when Enum.IsDefined(action.Action):
                if (TryTarget(action.Grid, action.Action, out var grid))
                    _ships.Run(this, grid, action.Action, action.Value, action.Notify);
                break;
        }
    }

    /// <summary>Selects a ship, or none, and sends its detail.</summary>
    public void Select(EntityUid? grid)
    {
        Selected = grid;
        StateDirty();
    }

    /// <summary>Sends the list again with the next state.</summary>
    public void RefreshList()
    {
        _sendList = true;
        StateDirty();
    }

    /// <summary>Sends the selected ship's detail again if anything in it changed.</summary>
    public void RefreshDetail()
    {
        if (!Pack(Detail()).AsSpan().SequenceEqual(_sentDetail))
            StateDirty();
    }

    /// <summary>Shows the outcome of an action in the panel.</summary>
    public void SetResult(string result, bool error = false)
    {
        _result = result;
        _resultError = error;
        _resultSeq++;
        StateDirty();
    }

    /// <summary>Keeps a scan's value for as long as the ship stays selected.</summary>
    public void Scanned(EntityUid grid, int value)
    {
        _scanned = grid;
        _value = value;
    }

    /// <summary>
    /// The grid a message names, when it still exists and the viewer may run the action on it; otherwise the
    /// refusal goes to the result line.
    /// </summary>
    public bool TryTarget(NetEntity target, WFShipAdminAction action, out EntityUid grid)
    {
        grid = default;
        if (!_ships.Allowed(Player, action))
        {
            SetResult(Loc.GetString("wf-ship-admin-result-no-permission"), true);
            return false;
        }

        if (!_entMan.TryGetEntity(target, out var found) || _ships.BuildDetail(found.Value, null) == null)
        {
            SetResult(Loc.GetString("wf-ship-admin-result-gone"), true);
            return false;
        }

        grid = found.Value;
        return true;
    }

    private WFShipAdminDetail? Detail()
    {
        if (Selected is not { } grid)
            return null;

        var detail = _ships.BuildDetail(grid, _scanned == grid ? _value : null);
        if (detail == null)
            Selected = null;

        return detail;
    }

    private byte[] Pack(WFShipAdminDetail? detail)
    {
        if (detail == null)
            return Array.Empty<byte>();

        using var stream = new MemoryStream();
        _serializer.Serialize(stream, detail);
        return stream.ToArray();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (_admin.HasAdminFlag(Player, WFShipAdminPermissions.Base))
            StateDirty();
        else
            Close();
    }
}
