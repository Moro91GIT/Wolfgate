using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._WF.PixelShift;

/// <summary>
/// Shifts and tilts a mob's own sprite while a pixel shift key is held. <see cref="SharedMoverController"/> hands it
/// the movement keys pressed meanwhile; a movement key pressed without one clears the shift.
/// </summary>
public sealed class WFPixelShiftSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>Degrees one tap of the tilt key turns the sprite.</summary>
    public const int TiltStep = 5;

    private int _maxOffset;
    private int _maxTilt;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, WFPixelShiftCVars.MaxOffset, v => _maxOffset = Math.Max(0, v), true);
        Subs.CVar(_cfg, WFPixelShiftCVars.MaxTilt, v => _maxTilt = Math.Clamp(v, 0, 180), true);

        CommandBinds.Builder
            .Bind(WFPixelShiftKeys.WFPixelShift, new HoldInputCmdHandler(this, WFPixelShiftMode.Shift))
            .Bind(WFPixelShiftKeys.WFPixelTilt, new HoldInputCmdHandler(this, WFPixelShiftMode.Tilt))
            .Bind(WFPixelShiftKeys.WFPixelShiftReset, InputCmdHandler.FromDelegate(session =>
            {
                if (session?.AttachedEntity is { } uid)
                    Reset(uid);
            }, handle: false, outsidePrediction: false))
            .Register<WFPixelShiftSystem>();

        SubscribeLocalEvent<WFPixelShiftHeldComponent, PlayerDetachedEvent>((uid, _, _) => RemComp<WFPixelShiftHeldComponent>(uid));
        SubscribeLocalEvent<WFPixelShiftHeldComponent, LocalPlayerDetachedEvent>((uid, _, _) => RemComp<WFPixelShiftHeldComponent>(uid));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<WFPixelShiftSystem>();
    }

    /// <summary>Holds or releases a pixel shift key. Holding one stops the mob where it stands.</summary>
    public void SetHeld(EntityUid uid, WFPixelShiftMode mode, bool held, ushort subTick = 0)
    {
        if (!held)
        {
            // Releasing the other key leaves this one held.
            if (TryComp<WFPixelShiftHeldComponent>(uid, out var current) && current.Mode == mode)
                RemComp<WFPixelShiftHeldComponent>(uid);
            return;
        }

        var wasHeld = HasComp<WFPixelShiftHeldComponent>(uid);
        EnsureComp<WFPixelShiftHeldComponent>(uid).Mode = mode;
        if (!wasHeld)
            _mover.WfReleaseDirections(uid, subTick);
    }

    /// <summary>
    /// Nudges the sprite for a movement key pressed while a pixel shift key is held. False when no key is held, so the
    /// key moves the mob as usual.
    /// </summary>
    public bool TryNudge(EntityUid uid, Direction dir)
    {
        if (!TryComp<WFPixelShiftHeldComponent>(uid, out var held))
            return false;

        if (!_blocker.CanInteract(uid, null))
            return true;

        WFPixelShiftComponent? shift;
        if (_net.IsServer)
            shift = EnsureComp<WFPixelShiftComponent>(uid);
        else if (!TryComp(uid, out shift))
            return true; // The server adds it; prediction starts from the next tap.

        if (held.Mode == WFPixelShiftMode.Shift)
        {
            var step = dir.ToIntVec();
            shift.Offset = new Vector2i(
                Math.Clamp(shift.Offset.X + step.X, -_maxOffset, _maxOffset),
                Math.Clamp(shift.Offset.Y + step.Y, -_maxOffset, _maxOffset));
        }
        else
        {
            var step = dir switch
            {
                Direction.West => TiltStep,
                Direction.East => -TiltStep,
                _ => 0,
            };
            shift.Tilt = Math.Clamp(shift.Tilt + step, -_maxTilt, _maxTilt);
        }

        Dirty(uid, shift);
        return true;
    }

    /// <summary>Clears the shift and tilt when the mob steps off, as long as it can walk.</summary>
    public void ResetOnWalk(EntityUid uid)
    {
        if (HasComp<WFPixelShiftComponent>(uid) && _blocker.CanMove(uid))
            Reset(uid);
    }

    /// <summary>Clears the shift and tilt.</summary>
    public void Reset(EntityUid uid)
    {
        if (!TryComp<WFPixelShiftComponent>(uid, out var shift) || shift.Offset == Vector2i.Zero && shift.Tilt == 0)
            return;

        shift.Offset = Vector2i.Zero;
        shift.Tilt = 0;
        Dirty(uid, shift);
    }

    private sealed class HoldInputCmdHandler(WFPixelShiftSystem system, WFPixelShiftMode mode) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid)
                return false;

            system.SetHeld(uid, mode, message.State == BoundKeyState.Down, message.SubTick);
            return false;
        }
    }
}
