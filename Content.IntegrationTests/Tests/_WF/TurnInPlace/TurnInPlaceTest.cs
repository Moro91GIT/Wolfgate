#nullable enable
using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._WF.TurnInPlace;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._WF.TurnInPlace;

/// <summary>The held turn key turns the mob with the movement keys instead of moving it, on server and client.</summary>
public sealed class TurnInPlaceTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task HeldKeyTurnsWithoutMoving()
    {
        var start = ServerPosition();

        await SetKey(WFTurnInPlaceKeys.WFTurnInPlace, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.East));
        Assert.That(ClientFacing(), Is.EqualTo(Direction.East), "The client didn't predict the turn.");

        await Tap(EngineKeyFunctions.MoveUp);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.North));
        Assert.That(Vector2.Distance(start, ServerPosition()), Is.LessThan(0.01f), "Turning moved the mob.");

        await SetKey(WFTurnInPlaceKeys.WFTurnInPlace, BoundKeyState.Up);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.East));
        Assert.That(ServerPosition().X - start.X, Is.GreaterThan(0.3f), "Released, the key should move the mob again.");
    }

    [Test]
    public async Task PressingTheKeyStopsAWalkingMob()
    {
        // With gravity, so the mob stops on friction rather than drifting on.
        await Server.WaitPost(() =>
        {
            var gravity = SEntMan.EnsureComponent<GravityComponent>(MapData.Grid);
            gravity.Enabled = true;
            SEntMan.Dirty(MapData.Grid, gravity);
        });
        await RunTicks(5);

        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await RunTicks(20);
        await SetKey(WFTurnInPlaceKeys.WFTurnInPlace, BoundKeyState.Down);
        await RunTicks(5);

        var buttons = SEntMan.GetComponent<InputMoverComponent>(SPlayer).HeldMoveButtons;
        Assert.That(buttons & MoveButtons.AnyDirection, Is.EqualTo(MoveButtons.None),
            "The turn key didn't release the held direction.");
        var clientButtons = CEntMan.GetComponent<InputMoverComponent>(CPlayer).HeldMoveButtons;
        Assert.That(clientButtons & MoveButtons.AnyDirection, Is.EqualTo(MoveButtons.None));

        await RunTicks(40);
        var stopped = ServerPosition();
        await RunTicks(20);
        Assert.That(Vector2.Distance(stopped, ServerPosition()), Is.LessThan(0.01f),
            "The mob kept walking with the turn key held.");

        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await SetKey(WFTurnInPlaceKeys.WFTurnInPlace, BoundKeyState.Up);
        await RunTicks(1);
        Assert.That(SEntMan.HasComponent<WFTurnInPlaceComponent>(SPlayer), Is.False);
    }

    private async Task Tap(BoundKeyFunction key)
    {
        await SetKey(key, BoundKeyState.Down);
        await RunTicks(20);
        await SetKey(key, BoundKeyState.Up);
        await RunTicks(5);
    }

    private Vector2 ServerPosition() => Transform.GetWorldPosition(SPlayer);

    private Direction ServerFacing() => Transform.GetWorldRotation(SPlayer).GetCardinalDir();

    private Direction ClientFacing()
    {
        return CEntMan.System<SharedTransformSystem>().GetWorldRotation(CPlayer).GetCardinalDir();
    }
}
