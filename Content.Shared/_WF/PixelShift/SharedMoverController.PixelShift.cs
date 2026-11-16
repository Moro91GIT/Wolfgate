using Content.Shared._WF.PixelShift;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    [Dependency] private WFPixelShiftSystem _wfPixelShift = default!;

    /// <summary>
    /// Hands a movement key press to the pixel shift while one of its keys is held. Otherwise the press is a step,
    /// which clears the shift.
    /// </summary>
    private bool WfTryPixelShift(EntityUid entity, Direction dir, bool state)
    {
        if (!state)
            return false;

        if (_wfPixelShift.TryNudge(entity, dir))
            return true;

        _wfPixelShift.ResetOnWalk(entity);
        return false;
    }
}
