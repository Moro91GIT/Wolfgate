using Content.Client._WF.Waddle;
using Content.Shared._WF.PixelShift;
using Robust.Client.GameObjects;

namespace Content.Client._WF.PixelShift;

/// <summary>Takes last frame's pixel shift off each sprite before animations and visualizers read it.</summary>
public sealed class WFPixelShiftRestoreSystem : EntitySystem
{
    [Dependency] private WFPixelShiftVisualsSystem _visuals = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesBefore.Add(typeof(AnimationPlayerSystem));
        UpdatesBefore.Add(typeof(AppearanceSystem));
        UpdatesAfter.Add(typeof(WaddleRestoreSystem));
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<WFPixelShiftComponent>();
        while (query.MoveNext(out var uid, out var shift))
        {
            _visuals.Remove((uid, shift));
        }
    }
}
