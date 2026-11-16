#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._WF.CancelInteractions;
using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.Tests._WF.CancelInteractions;

/// <summary>The cancel key stops the player's own do-after.</summary>
[TestOf(typeof(WFCancelInteractionSystem))]
public sealed partial class CancelInteractionsTest : InteractionTest
{
    [Serializable, NetSerializable]
    private sealed partial class WFCancelTestDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

    [Test]
    public async Task KeyCancelsOwnDoAfter()
    {
        var doAfter = SEntMan.System<SharedDoAfterSystem>();
        DoAfterId? id = null;

        await Server.WaitPost(() =>
        {
            var args = new DoAfterArgs(SEntMan, SPlayer, TimeSpan.FromSeconds(30), new WFCancelTestDoAfterEvent(), null) { Broadcast = true };
            Assert.That(doAfter.TryStartDoAfter(args, out id), Is.True);
        });
        await RunTicks(5);
        await Server.WaitAssertion(() => Assert.That(doAfter.GetStatus(id), Is.EqualTo(DoAfterStatus.Running)));

        await PressKey(WFCancelInteractionKeys.WFCancelInteractions);
        await RunTicks(5);
        await Server.WaitAssertion(() => Assert.That(doAfter.GetStatus(id), Is.EqualTo(DoAfterStatus.Cancelled)));
    }
}
