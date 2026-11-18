#nullable enable
using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._WG.TurnInPlace;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._WG.TurnInPlace;

/// <summary>The held turn key turns the mob with the movement keys instead of moving it, on server and client.</summary>
public sealed class TurnInPlaceTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task HeldKeyTurnsWithoutMoving()
    {
        var start = ServerPosition();

        await SetKey(WGTurnInPlaceKeys.WGTurnInPlace, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.East));
        Assert.That(ClientFacing(), Is.EqualTo(Direction.East), "The client didn't predict the turn.");

        await Tap(EngineKeyFunctions.MoveUp);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.North));
        Assert.That(Vector2.Distance(start, ServerPosition()), Is.LessThan(0.01f), "Turning moved the mob.");

        await SetKey(WGTurnInPlaceKeys.WGTurnInPlace, BoundKeyState.Up);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(ServerFacing(), Is.EqualTo(Direction.East));
        Assert.That(ServerPosition().X - start.X, Is.GreaterThan(0.3f), "Released, the key should move the mob again.");
    }

    [Test]
    public async Task PressingTheKeyStopsAWalkingMob()
    {
        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await RunTicks(20);
        await SetKey(WGTurnInPlaceKeys.WGTurnInPlace, BoundKeyState.Down);
        await RunTicks(5);

        var buttons = SEntMan.GetComponent<InputMoverComponent>(SPlayer).HeldMoveButtons;
        Assert.That(buttons & MoveButtons.AnyDirection, Is.EqualTo(MoveButtons.None),
            "The turn key didn't release the held direction.");
        var clientButtons = CEntMan.GetComponent<InputMoverComponent>(CPlayer).HeldMoveButtons;
        Assert.That(clientButtons & MoveButtons.AnyDirection, Is.EqualTo(MoveButtons.None));

        // The test map is weightless, so the mob coasts; what matters is that it stops pushing.
        await RunTicks(10);
        Assert.That(SEntMan.System<SharedMoverController>().GetWishDir(SPlayer), Is.EqualTo(Vector2.Zero),
            "The mob kept walking with the turn key held.");

        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await SetKey(WGTurnInPlaceKeys.WGTurnInPlace, BoundKeyState.Up);
        await RunTicks(10);
        Assert.That(SEntMan.HasComponent<WGTurnInPlaceComponent>(SPlayer), Is.False);
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
