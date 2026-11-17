using Content.Shared._WG.Tugs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Graphics.RSI;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._WG.Tugs;

/// <summary>Draws a tug over its rider when it faces up or down the screen, and under them side-on, like the ride-on ATV.</summary>
public sealed class WGTugVisualsSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var eyeRotation = _eye.CurrentEye.Rotation;
        var query = EntityQueryEnumerator<WGTugComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            var direction = SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, _transform.GetWorldRotation(xform) + eyeRotation);
            var depth = direction is RsiDirection.North or RsiDirection.South
                ? (int) DrawDepth.Doors
                : (int) DrawDepth.Objects;

            if (sprite.DrawDepth != depth)
                _sprite.SetDrawDepth((uid, sprite), depth);
        }
    }
}
