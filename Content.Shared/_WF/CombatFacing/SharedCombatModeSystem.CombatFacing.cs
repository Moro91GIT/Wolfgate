using Content.Shared._WF.CombatFacing;

namespace Content.Shared.CombatMode;

public abstract partial class SharedCombatModeSystem
{
    [Dependency] private WFCombatFacingSystem _wfCombatFacing = default!;

    /// <summary>Whether entering combat mode should turn this entity toward the cursor.</summary>
    private bool WfFacesCursor(EntityUid uid)
    {
        return _wfCombatFacing.WantsFaceCursor(uid);
    }

    /// <summary>Starts or stops facing the cursor for an entity already in combat mode.</summary>
    public void WfSetFaceCursor(EntityUid uid, bool faceCursor)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combat) || !combat.IsInCombatMode || !combat.ToggleMouseRotator)
            return;

        SetMouseRotatorComponents(uid, faceCursor);
    }
}
