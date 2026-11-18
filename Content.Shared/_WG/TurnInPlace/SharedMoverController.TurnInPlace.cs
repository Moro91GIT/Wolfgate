using Content.Shared._WG.TurnInPlace;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /// <summary>
    /// Turns the mob instead of moving it while its turn-in-place key is held. True when the input was used up.
    /// Key releases always pass through, so a key held before the turn key still clears.
    /// </summary>
    private bool WgTryTurnInPlace(EntityUid entity, Direction dir, bool state)
    {
        if (!state || !HasComp<WGTurnInPlaceComponent>(entity))
            return false;

        var target = WgMoverTarget(entity);
        if (!MoverQuery.TryComp(target, out var mover)
            || !mover.CanMove
            || NoRotateQuery.HasComp(target)
            || _container.IsEntityInContainer(target)
            || !XformQuery.TryComp(target, out var xform))
        {
            return true;
        }

        var vec = dir.ToVec();
        var wish = _relativeMovement ? GetParentGridAngle(mover).RotateVec(vec) : vec;
        var worldRot = _transform.GetWorldRotation(xform);
        _transform.SetLocalRotation(target, xform.LocalRotation + wish.ToWorldAngle() - worldRot, xform);
        return true;
    }

    /// <summary>Releases the direction keys of the mob an entity steers, so a held turn or shift key stops it.</summary>
    public void WgReleaseDirections(EntityUid entity, ushort subTick)
    {
        var target = WgMoverTarget(entity);
        if (MoverQuery.TryComp(target, out var mover))
            SetMoveInput((target, mover), subTick, false, MoveButtons.AnyDirection);
    }

    /// <summary>The mob an entity's movement keys steer, following movement relays.</summary>
    private EntityUid WgMoverTarget(EntityUid entity)
    {
        for (var i = 0; i < 4 && RelayQuery.TryComp(entity, out var relay); i++)
        {
            entity = relay.RelayEntity;
        }

        return entity;
    }
}
