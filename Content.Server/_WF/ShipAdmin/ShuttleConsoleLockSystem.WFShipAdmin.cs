using System.Linq;
using Content.Server.Shuttles.Components;
using Content.Shared._WF.ShipAccess;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Audio;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Lets whoever manages a ship without holding its deed card work the helm lock: a player the ship is registered
/// to, a holder of an owner's card, or an admin. The upstream lock only knows deed cards and vouchers.
/// </summary>
public sealed partial class ShuttleConsoleLockSystem
{
    [Dependency] private WFShipAccessSystem _wfAccessRules = default!;

    private static readonly SoundSpecifier WfSwipeSound = new SoundPathSpecifier("/Audio/Machines/id_swipe.ogg");

    /// <summary>Whether the user manages the ship this console is on.</summary>
    private bool WfIsManager(EntityUid console, EntityUid user)
    {
        return Transform(console).GridUid is { } grid && _wfShipAccess.IsOwner(user, grid);
    }

    /// <summary>
    /// Toggles the helm lock for a manager who holds no deed for the ship. A deed holder is left to the swipe below,
    /// and so is anyone else, who gets the wrong-deed popup or guest access from it.
    /// </summary>
    /// <returns>True when the toggle was handled here.</returns>
    private bool WfTryManagerToggle(EntityUid console, EntityUid user, ShuttleConsoleLockComponent lockComp, bool locked)
    {
        if (Transform(console).GridUid is not { } grid || _wfAccessRules.HasDeedFor(user, grid) || !_wfShipAccess.IsOwner(user, grid))
            return false;

        var gridLock = CompOrNull<ShipGridLockComponent>(grid);
        var shuttleId = gridLock?.ShuttleId ?? lockComp.ShuttleId;

        // A console with no ship behind it can't be locked; the swipe below says so.
        if (!locked && string.IsNullOrEmpty(shuttleId))
            return false;

        if (gridLock != null)
            SetGridLockState(grid, !locked, locked ? null : shuttleId);
        else
            lockComp.Locked = !locked;

        _audio.PlayPvs(WfSwipeSound, console);
        Popup.PopupEntity(Loc.GetString(locked ? "shuttle-console-unlocked" : "shuttle-console-locked-success"), console);
        if (locked || !TryComp<ShuttleConsoleComponent>(console, out var shuttleComp))
            return true;

        foreach (var pilot in shuttleComp.SubscribedPilots.ToList())
            _consoleSystem.RemovePilot(pilot);

        return true;
    }
}
