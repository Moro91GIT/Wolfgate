using Content.Shared._WF.PixelShift;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    [Dependency] private WFPixelShiftSystem _wfPixelShift = default!;

    /// <summary>Hands a movement key press to the pixel shift while one of its keys is held.</summary>
    private bool WfTryPixelShift(EntityUid entity, Direction dir, bool state)
    {
        return state && _wfPixelShift.TryNudge(entity, dir);
    }
}
