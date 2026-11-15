using System;
using Content.Shared._WF.ShipAdmin;
using Content.Shared.Administration;
using NUnit.Framework;

namespace Content.Tests._WF.ShipAdmin;

/// <summary>The ship panel's flag table: Admin for most actions, Debug on top for the ones that are hard to undo.</summary>
[TestFixture]
[TestOf(typeof(WFShipAdminPermissions))]
public sealed class ShipAdminPermissionsTest
{
    private static readonly WFShipAdminAction[] ElevatedActions =
    {
        WFShipAdminAction.Release,
        WFShipAdminAction.DetachHangar,
        WFShipAdminAction.Restore,
        WFShipAdminAction.Strip,
        WFShipAdminAction.Delete,
    };

    [Test]
    public void AdminRunsEverythingButTheElevatedActions()
    {
        foreach (var action in Enum.GetValues<WFShipAdminAction>())
        {
            var elevated = Array.IndexOf(ElevatedActions, action) >= 0;
            Assert.That(WFShipAdminPermissions.Allowed(AdminFlags.Admin, action), Is.EqualTo(!elevated), action.ToString());
        }
    }

    [Test]
    public void DebugOnTopRunsEverything()
    {
        foreach (var action in Enum.GetValues<WFShipAdminAction>())
            Assert.That(WFShipAdminPermissions.Allowed(AdminFlags.Admin | AdminFlags.Debug | AdminFlags.Fun, action), Is.True, action.ToString());
    }

    [Test]
    public void NothingRunsWithoutAdmin()
    {
        foreach (var action in Enum.GetValues<WFShipAdminAction>())
        {
            Assert.Multiple(() =>
            {
                Assert.That(WFShipAdminPermissions.Allowed(AdminFlags.None, action), Is.False, action.ToString());
                Assert.That(WFShipAdminPermissions.Allowed(AdminFlags.Debug | AdminFlags.Spawn, action), Is.False, action.ToString());
            });
        }
    }
}
