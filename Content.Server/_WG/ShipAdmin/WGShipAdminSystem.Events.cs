using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared._WG.ShipAdmin;
using Content.Shared.Database;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._WG.ShipAdmin;

public sealed partial class WGShipAdminSystem
{
    [Dependency] private GameTicker _ticker = default!;

    /// <summary>
    /// Events that happen aboard a station and pick it, or a tile on it, through the game rule pickers that honour
    /// <see cref="WGEventTargetComponent"/>. Sector-wide events and those that pick their target another way are left out.
    /// </summary>
    private static readonly string[] AboardEvents =
    {
        "GasLeak",
        "VentClog",
        "MouseMigration",
        "KingRatMigration",
        "CockroachMigration",
        "SnailMigration",
        "RandomSentience",
        "ImmovableRodSpawn",
        "BluespaceCargoCrate",
    };

    private List<WGShipAdminEvent>? _events;

    /// <summary>
    /// The events the panel offers: those that happen aboard, and every station event that loads ships (raiders and
    /// strangers), which arrive beside the target.
    /// </summary>
    public List<WGShipAdminEvent> ListEvents()
    {
        if (_events != null)
            return _events;

        _events = new List<WGShipAdminEvent>();
        foreach (var id in AboardEvents)
        {
            if (_proto.TryIndex<EntityPrototype>(id, out var proto))
                _events.Add(new WGShipAdminEvent { Id = id, Name = EventName(proto) });
        }

        var factory = EntityManager.ComponentFactory;
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>()
                     .Where(proto => !proto.Abstract
                                     && proto.TryGetComponent<LoadMapRuleComponent>(out _, factory)
                                     && proto.TryGetComponent<StationEventComponent>(out _, factory))
                     .OrderBy(proto => proto.ID))
        {
            _events.Add(new WGShipAdminEvent { Id = proto.ID, Name = EventName(proto), Arrival = true });
        }

        return _events;
    }

    /// <summary>
    /// Starts an event aimed at a ship. One that happens aboard picks the ship's station and a tile on the ship, or
    /// fails without falling back to another station; one that brings ships puts them beside it.
    /// </summary>
    public bool StartEvent(ICommonSession admin, EntityUid grid, string rule, out string result)
    {
        var choice = ListEvents().FirstOrDefault(e => e.Id == rule);
        if (choice == null)
        {
            result = Loc.GetString("wg-ship-admin-result-bad-event");
            return false;
        }

        var station = _station.GetOwningStation(grid);
        if (!choice.Arrival && station == null)
        {
            result = Loc.GetString("wg-ship-admin-result-event-no-station", ("ship", Name(grid)));
            return false;
        }

        var ruleEntity = _ticker.AddGameRule(rule);
        if (choice.Arrival)
        {
            // The ships load when the rule is added; bring each beside the target before the rule starts.
            if (TryComp<RuleGridsComponent>(ruleEntity, out var loaded))
            {
                foreach (var arrived in loaded.MapGrids)
                {
                    if (HasComp<MapGridComponent>(arrived))
                        _shuttle.TryFTLProximity(arrived, grid);
                }
            }
        }
        else
        {
            var target = EnsureComp<WGEventTargetComponent>(ruleEntity);
            target.Station = station!.Value;
            target.Grid = grid;
        }

        _ticker.StartGameRule(ruleEntity);
        Audit(admin, LogImpact.High, grid, $"started the event {rule} aimed at it");
        result = Loc.GetString(choice.Arrival ? "wg-ship-admin-result-event-arrival" : "wg-ship-admin-result-event",
            ("event", choice.Name), ("ship", Name(grid)));
        return true;
    }

    private static string EventName(EntityPrototype proto)
    {
        return string.IsNullOrWhiteSpace(proto.Name) ? proto.ID : $"{proto.Name} ({proto.ID})";
    }
}
