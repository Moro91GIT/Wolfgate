#nullable enable
using System.Numerics;
using Content.Client._WF.PixelShift;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Cuffs;
using Content.Shared._WF.PixelShift;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._WF.PixelShift;

/// <summary>
/// The pixel shift and tilt keys nudge the networked shift, which clamps, is blocked when helpless and clears on a reset
/// or a step.
/// </summary>
public sealed class PixelShiftTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task ShiftAndTiltUntilReset()
    {
        var start = Transform.GetWorldPosition(SPlayer);

        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveRight, 3);
        await Tap(EngineKeyFunctions.MoveUp);
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Up);

        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveLeft, 2);
        await Tap(EngineKeyFunctions.MoveRight);
        await Tap(EngineKeyFunctions.MoveUp);
        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Up);

        var shift = SEntMan.GetComponent<WFPixelShiftComponent>(SPlayer);
        Assert.That(shift.Offset, Is.EqualTo(new Vector2i(3, 1)));
        Assert.That(shift.Tilt, Is.EqualTo(WFPixelShiftSystem.TiltStep));
        Assert.That(Vector2.Distance(start, Transform.GetWorldPosition(SPlayer)), Is.LessThan(0.01f),
            "Shifting moved the mob.");

        var clientShift = CEntMan.GetComponent<WFPixelShiftComponent>(CPlayer);
        Assert.That(clientShift.Offset, Is.EqualTo(new Vector2i(3, 1)), "The client never got the shift.");
        await Client.WaitPost(() =>
        {
            // The client may already have run its frame passes; start from the restored sprite either way.
            CEntMan.System<WFPixelShiftRestoreSystem>().FrameUpdate(0f);
            CEntMan.System<WFPixelShiftVisualsSystem>().FrameUpdate(0f);
            var sprite = CEntMan.GetComponent<SpriteComponent>(CPlayer);
            Assert.That(sprite.Offset.X, Is.EqualTo(3f / EyeManager.PixelsPerMeter).Within(0.001f));
            Assert.That(sprite.Rotation.Degrees, Is.EqualTo(WFPixelShiftSystem.TiltStep).Within(0.001));
            CEntMan.System<WFPixelShiftRestoreSystem>().FrameUpdate(0f);
            Assert.That(sprite.Offset.X, Is.EqualTo(0f).Within(0.001f), "The restore pass left the shift on.");
        });

        await Tap(WFPixelShiftKeys.WFPixelShiftReset);
        Assert.That(shift.Offset, Is.EqualTo(Vector2i.Zero));
        Assert.That(shift.Tilt, Is.Zero);
    }

    [Test]
    public async Task ReleasingOneKeyKeepsTheOther()
    {
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Down);
        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveLeft);
        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Up);
        await Tap(EngineKeyFunctions.MoveRight);
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Up);

        var shift = SEntMan.GetComponent<WFPixelShiftComponent>(SPlayer);
        Assert.That(shift.Tilt, Is.EqualTo(WFPixelShiftSystem.TiltStep), "The later key didn't take over.");
        Assert.That(shift.Offset, Is.EqualTo(new Vector2i(1, 0)), "Releasing tilt dropped the held shift key.");
    }

    [Test]
    public async Task WalkingClearsTheShift()
    {
        var start = Transform.GetWorldPosition(SPlayer);

        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveUp, 2);
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Up);
        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveLeft);
        await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Up);
        Assert.That(Offset(), Is.EqualTo(new Vector2i(0, 2)));

        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await RunTicks(20);
        await SetKey(EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await RunTicks(5);

        Assert.That(Transform.GetWorldPosition(SPlayer).X - start.X, Is.GreaterThan(0.3f));
        var shift = SEntMan.GetComponent<WFPixelShiftComponent>(SPlayer);
        Assert.That(shift.Offset, Is.EqualTo(Vector2i.Zero), "Walking left the shift on.");
        Assert.That(shift.Tilt, Is.Zero, "Walking left the tilt on.");
        Assert.That(CEntMan.GetComponent<WFPixelShiftComponent>(CPlayer).Offset, Is.EqualTo(Vector2i.Zero),
            "The client kept the shift.");
    }

    [Test]
    public async Task ShiftIsClamped()
    {
        var cfg = Server.ResolveDependency<IConfigurationManager>();
        await Server.WaitPost(() =>
        {
            cfg.SetCVar(WFPixelShiftCVars.MaxOffset, 2);
            cfg.SetCVar(WFPixelShiftCVars.MaxTilt, 10);
        });
        await RunTicks(5);

        try
        {
            await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Down);
            await Tap(EngineKeyFunctions.MoveLeft, 4);
            await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Up);
            await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Down);
            await Tap(EngineKeyFunctions.MoveRight, 4);
            await SetKey(WFPixelShiftKeys.WFPixelTilt, BoundKeyState.Up);

            var shift = SEntMan.GetComponent<WFPixelShiftComponent>(SPlayer);
            Assert.That(shift.Offset, Is.EqualTo(new Vector2i(-2, 0)));
            Assert.That(shift.Tilt, Is.EqualTo(-10));
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                cfg.SetCVar(WFPixelShiftCVars.MaxOffset, WFPixelShiftCVars.MaxOffset.DefaultValue);
                cfg.SetCVar(WFPixelShiftCVars.MaxTilt, WFPixelShiftCVars.MaxTilt.DefaultValue);
            });
        }
    }

    [Test]
    public async Task RestrainedOrDeadCantShift()
    {
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Down);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(Offset(), Is.EqualTo(new Vector2i(1, 0)));

        await Server.WaitPost(() =>
        {
            var cuffs = SEntMan.SpawnAtPosition("Handcuffs", SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates);
            Assert.That(SEntMan.System<CuffableSystem>().TryAddNewCuffs(SPlayer, SPlayer, cuffs), Is.True);
        });
        await RunTicks(5);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(Offset(), Is.EqualTo(new Vector2i(1, 0)), "A cuffed mob shifted its sprite.");

        await Server.WaitPost(() => SEntMan.System<MobStateSystem>().ChangeMobState(SPlayer, MobState.Dead));
        await RunTicks(5);
        await Tap(EngineKeyFunctions.MoveRight);
        Assert.That(Offset(), Is.EqualTo(new Vector2i(1, 0)), "A dead mob shifted its sprite.");
        await SetKey(WFPixelShiftKeys.WFPixelShift, BoundKeyState.Up);
    }

    private Vector2i Offset()
    {
        return SEntMan.TryGetComponent<WFPixelShiftComponent>(SPlayer, out var shift) ? shift.Offset : Vector2i.Zero;
    }

    private async Task Tap(BoundKeyFunction key, int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            await SetKey(key, BoundKeyState.Down);
            await RunTicks(2);
            await SetKey(key, BoundKeyState.Up);
            await RunTicks(3);
        }
    }
}
