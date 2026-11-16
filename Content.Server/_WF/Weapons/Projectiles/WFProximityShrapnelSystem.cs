using System.Numerics;
using Content.Shared.Projectiles;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;

namespace Content.Server._WF.Weapons.Projectiles;

/// <summary>Keeps wide-circle shrapnel from hitting through walls.</summary>
public sealed class WFProximityShrapnelSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFProximityShrapnelComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<WFProximityShrapnelComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;
        if (args.Cancelled || HasComp<ProjectileComponent>(other) || Transform(other).GridUid == null)
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixtures) ||
            !fixtures.Fixtures.TryGetValue(SharedProjectileSystem.ProjectileFixture, out var fixture) || fixture != args.OurFixture)
            return;

        var at = _transform.GetWorldPosition(ent);
        var box = _lookup.GetWorldAABB(other);
        var nearest = Vector2.Clamp(at, box.BottomLeft, box.TopRight);
        if ((nearest - at).LengthSquared() > ent.Comp.Reach * ent.Comp.Reach)
            args.Cancelled = true;
    }
}
