using Content.Client._WF.Notifications;
using NUnit.Framework;

namespace Content.Tests._WF.Notifications;

/// <summary>Only a rise in the open ghost role count, after the first count, is a new role.</summary>
[TestFixture]
[TestOf(typeof(WFGhostRoleAlertSystem))]
public sealed class WFGhostRoleAlertTest
{
    [TestCase(null, 3, false, TestName = "First count after becoming a ghost")]
    [TestCase(2, 3, true, TestName = "Count rose")]
    [TestCase(3, 3, false, TestName = "Count unchanged")]
    [TestCase(3, 1, false, TestName = "Count fell")]
    public void IsNewRole(int? last, int now, bool expected)
    {
        Assert.That(WFGhostRoleAlertSystem.IsNewRole(last, now), Is.EqualTo(expected));
    }
}
