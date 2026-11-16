using System.Numerics;
using Content.Client._CE.ZLevels.Core;
using Content.Client._WF.Waddle;
using Content.Shared._WF.PixelShift;
using Content.Shared.Buckle;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._WF.PixelShift;

/// <summary>
/// Adds each mob's pixel shift and tilt to its sprite after animations have set it, so lunges, jitter and lying down
/// still play from the mob's own base. <see cref="WFPixelShiftRestoreSystem"/> takes it back off before the next frame.
/// </summary>
public sealed class WFPixelShiftVisualsSystem : EntitySystem
{
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(AnimationPlayerSystem));
        UpdatesAfter.Add(typeof(AppearanceSystem));
        // Waddle stacks on top of the shift, so it comes off first.
        UpdatesBefore.Add(typeof(WaddleSystem));
        UpdatesBefore.Add(typeof(CEClientZLevelsPostAnimSystem));

        SubscribeLocalEvent<WFPixelShiftComponent, ComponentShutdown>((uid, comp, _) => Remove((uid, comp)));
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<WFPixelShiftComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var shift, out var sprite))
        {
            var offset = (Vector2) shift.Offset / EyeManager.PixelsPerMeter;
            // Lying down and buckling own the sprite's rotation.
            var tilt = shift.Tilt == 0 || _standing.IsDown(uid) || _buckle.IsBuckled(uid)
                ? Angle.Zero
                : Angle.FromDegrees(shift.Tilt);

            shift.AppliedOffset = offset;
            shift.AppliedTilt = tilt;
            if (offset == Vector2.Zero && tilt == Angle.Zero)
                continue;

            shift.WrittenOffset = sprite.Offset + offset;
            shift.WrittenRotation = sprite.Rotation + tilt;
            _sprite.SetOffset((uid, sprite), shift.WrittenOffset);
            _sprite.SetRotation((uid, sprite), shift.WrittenRotation);
        }
    }

    /// <summary>Takes this frame's shift and tilt back off, unless something has set the sprite since.</summary>
    public void Remove(Entity<WFPixelShiftComponent> ent)
    {
        var shift = ent.Comp;
        if (shift.AppliedOffset == Vector2.Zero && shift.AppliedTilt == Angle.Zero)
            return;

        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            if (sprite.Offset.Equals(shift.WrittenOffset))
                _sprite.SetOffset((ent, sprite), sprite.Offset - shift.AppliedOffset);

            if (sprite.Rotation.Equals(shift.WrittenRotation))
                _sprite.SetRotation((ent, sprite), sprite.Rotation - shift.AppliedTilt);
        }

        shift.AppliedOffset = Vector2.Zero;
        shift.AppliedTilt = Angle.Zero;
    }

    /// <summary>
    /// The shift currently in a sprite's offset, for systems that record the offset as a base to return to.
    /// </summary>
    public Vector2 CurrentShift(EntityUid uid, SpriteComponent sprite)
    {
        return TryComp<WFPixelShiftComponent>(uid, out var shift) && sprite.Offset.Equals(shift.WrittenOffset)
            ? shift.AppliedOffset
            : Vector2.Zero;
    }
}
