using System.Numerics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.Projectiles;

public sealed partial class ProjectileSystem
{
    [Dependency] private IConfigurationManager _wgCfg = default!;
    [Dependency] private SharedMapSystem _wgMap = default!;

    /// <summary>Below this speed relative to the sweep's frame, a grid is left to the frame's own ray.</summary>
    private const float WgMovingGridSpeed = 0.1f;

    private List<Entity<MapGridComponent>> _wgSweepGrids = new();
    private readonly HashSet<EntityUid> _wgMovingGrids = new();
    private readonly List<WgSweepHit> _wgSweepHits = new();

    /// <summary>A sweep hit, with the direction it was swept along and the fraction of a second until impact.</summary>
    private readonly record struct WgSweepHit(EntityUid Entity, float Distance, Vector2 Direction, float Time);

    /// <summary>
    /// Sweeps one tick of travel, casting each grid that moves in this frame with the projectile's velocity relative to it.
    /// A single ray in the parent's frame misses a hull that closes the gap by more than its own thickness in a tick.
    /// </summary>
    private List<WgSweepHit> WgSweep(EntityUid uid, MapCoordinates from, Vector2 velocity, Vector2 frameVelocity,
        float frameTime, int mask)
    {
        _wgSweepHits.Clear();
        _wgMovingGrids.Clear();
        _wgSweepGrids.Clear();

        // A grid can reach the path from as far as it moves in a tick at the physics speed limit.
        var world = velocity + frameVelocity;
        var bounds = Box2.FromTwoPoints(from.Position, from.Position + world * frameTime)
            .Enlarged(_wgCfg.GetCVar(CVars.MaxLinVelocity) * frameTime);
        _wgMap.FindGridsIntersecting(from.MapId, bounds, ref _wgSweepGrids);

        foreach (var grid in _wgSweepGrids)
        {
            if (!_physQuery.TryComp(grid, out var body))
                continue;

            // Translational only: a spinning grid's speed at the round is not the speed of the hull it will meet.
            var gridVelocity = _physics.GetMapLinearVelocity(grid, body);
            if ((gridVelocity - frameVelocity).Length() < WgMovingGridSpeed)
                continue;

            // A spinning grid keeps its hits on the frame's ray too, so its turn cannot hide a wall from both rays.
            var spin = MathF.Abs(body.AngularVelocity) * (from.Position - _transformSystem.GetWorldPosition(grid)).Length();
            if (spin < WgMovingGridSpeed)
                _wgMovingGrids.Add(grid.Owner);

            WgCast(uid, from, world - gridVelocity, frameTime, mask, grid.Owner);
        }

        WgCast(uid, from, velocity, frameTime, mask, null);
        return _wgSweepHits;
    }

    /// <summary>Casts one ray, keeping hits on <paramref name="grid"/>, or with no grid, hits off every grid swept on its own.</summary>
    private void WgCast(EntityUid uid, MapCoordinates from, Vector2 velocity, float frameTime, int mask, EntityUid? grid)
    {
        var speed = velocity.Length();
        if (speed <= 0f)
            return;

        var direction = velocity / speed;
        var ray = new CollisionRay(from.Position, direction, mask);
        foreach (var hit in _physics.IntersectRay(from.MapId, ray, speed * frameTime, uid, false))
        {
            var hitGrid = Transform(hit.HitEntity).GridUid;
            if (grid != null ? hitGrid != grid : hitGrid is { } moving && _wgMovingGrids.Contains(moving))
                continue;

            _wgSweepHits.Add(new WgSweepHit(hit.HitEntity, hit.Distance, direction, hit.Distance / speed));
        }
    }
}
