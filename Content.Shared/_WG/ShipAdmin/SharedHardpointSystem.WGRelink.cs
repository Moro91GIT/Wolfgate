using Robust.Shared.Map.Components;

namespace Content.Shared._Crescent.Hardpoints;

public partial class SharedHardpointSystem
{
    /// <summary>A hardpoint spawned under a gun that is already in place, as a rebuild does, takes that gun.</summary>
    private void WgOnHardpointMapInit(Entity<HardpointComponent> hardpoint, ref MapInitEvent args)
    {
        WgLinkGunsOnTile(hardpoint);
    }

    /// <summary>
    /// Links every gun on the hardpoint's tile that isn't mounted on a live hardpoint. Upstream only a gun looks for
    /// a hardpoint, so one laid or rebuilt after its gun never linked it, and the gun never joined gunnery.
    /// </summary>
    public void WgLinkGunsOnTile(Entity<HardpointComponent> hardpoint)
    {
        if (hardpoint.Comp.anchoring is { } held && !TerminatingOrDeleted(held))
            return;

        var xform = Transform(hardpoint);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _mapSystem.TileIndicesFor(gridUid, grid, xform.Coordinates);
        foreach (var entity in _mapSystem.GetAnchoredEntities((gridUid, grid), tile))
        {
            if (!TryComp<HardpointAnchorableOnlyComponent>(entity, out var gun) || WgIsMounted(gun))
                continue;

            gun.anchoredTo = null;
            if (TryAnchorToHardpoint(entity, gun))
                return;
        }
    }

    /// <summary>Whether a gun is mounted on a hardpoint that still exists.</summary>
    public bool WgIsMounted(HardpointAnchorableOnlyComponent gun)
    {
        return gun.anchoredTo is { } on && !TerminatingOrDeleted(on);
    }
}
