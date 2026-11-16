#nullable enable
using System.Collections.Generic;
using System.Numerics;
using Content.Server._WF.RadarMenu;
using Content.Server.NPC.HTN;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._WF.RadarMenu;

/// <summary>"Dock at" from the radar menu: lists the docks a ship fits, flies to one and docks on arrival.</summary>
[TestFixture]
[TestOf(typeof(WFAutoDockSystem))]
public sealed class WFAutoDockTest
{
    [Test]
    public async Task ShipDocksWhereTheAutopilotTookIt()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.EntMan;
        var maps = entities.System<SharedMapSystem>();
        var xform = entities.System<SharedTransformSystem>();
        var autoDock = entities.System<WFAutoDockSystem>();

        await server.WaitAssertion(() =>
        {
            entities.DeleteEntity(map.Grid);

            // The ship: a vertical I with a dock at its foot and a helm on it.
            var ship = maps.CreateGridEntity(map.MapId);
            maps.SetTiles(ship.Owner, ship.Comp, new List<(Vector2i, Tile)>
            {
                (new Vector2i(0, 0), new Tile(1)), (new Vector2i(0, 1), new Tile(1)), (new Vector2i(0, 2), new Tile(1)),
            });
            entities.EnsureComponent<ShuttleComponent>(ship.Owner);
            var shipDock = entities.SpawnEntity("AirlockShuttle", new EntityCoordinates(ship.Owner, new Vector2(0.5f, 0.5f)));
            var console = entities.SpawnEntity("ComputerShuttle", new EntityCoordinates(ship.Owner, new Vector2(0.5f, 2.5f)));
            entities.GetComponent<HTNComponent>(console).Enabled = false;

            // The station: a T with a free dock, 70 m off.
            var station = maps.CreateGridEntity(map.MapId);
            xform.SetLocalPosition(station.Owner, new Vector2(50f, 50f));
            maps.SetTiles(station.Owner, station.Comp, new List<(Vector2i, Tile)>
            {
                (new Vector2i(0, 0), new Tile(1)), (new Vector2i(0, 1), new Tile(1)), (new Vector2i(0, 2), new Tile(1)),
                (new Vector2i(-1, 2), new Tile(1)), (new Vector2i(1, 2), new Tile(1)),
            });
            var stationDock = entities.SpawnEntity("AirlockShuttle", new EntityCoordinates(station.Owner, new Vector2(0.5f, 0.5f)));

            var options = autoDock.Options(console, station.Owner);
            Assert.That(options, Has.Count.EqualTo(1), "The station's one free dock fits the ship.");
            Assert.That(options[0].Dock, Is.EqualTo(entities.GetNetEntity(stationDock)));
            Assert.That(autoDock.Options(console, ship.Owner), Is.Empty, "A ship cannot dock with itself.");

            var helm = entities.GetComponent<ShuttleConsoleComponent>(console);
            Assert.That(autoDock.TryStart((console, helm), station.Owner, stationDock, null), Is.True);
            var htn = entities.GetComponent<HTNComponent>(console);
            Assert.That(htn.Blackboard.TryGetValue<EntityCoordinates>(helm.AutopilotTargetKey, out var target, entities), Is.True,
                "The autopilot has the docked place as its target.");
            Assert.That(target.EntityId, Is.EqualTo(station.Owner), "The target moves with the station.");
            var request = entities.GetComponent<WFAutoDockComponent>(console);

            // Stopped short: no docking, and the reason says so.
            Assert.That(autoDock.TryFinish(console, request, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo("wf-radar-menu-dock-failed-short"));

            // Arrived within the autopilot's range: the clamps pull it in.
            var docked = xform.ToMapCoordinates(target).Position;
            xform.SetWorldPosition(ship.Owner, docked + new Vector2(0f, -20f));
            Assert.That(autoDock.TryFinish(console, request, out reason), Is.True, reason);
            Assert.That(entities.GetComponent<DockingComponent>(shipDock).DockedWith, Is.EqualTo(stationDock));
            Assert.That(Vector2.Distance(xform.GetWorldPosition(ship.Owner), docked), Is.LessThan(0.01f));

            // Docked, it lists nothing new to fly to and refuses to set off.
            Assert.That(autoDock.TryStart((console, helm), station.Owner, null, null), Is.False, "Undock first.");
        });

        await pair.CleanReturnAsync();
    }
}
