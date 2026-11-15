using System.Linq;
using Content.Server._WF.Ordnance;
using Content.Server._WF.Warheads;
using Content.Server._WF.Warheads.Components;
using Content.Server.Wires;
using Content.Shared._WF.Warheads;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WF.Ordnance;

/// <summary>
/// The faction warhead counts down for at least 90 s, sounds its klaxon only while armed, and behind its panel has two
/// wires: one makes it safe, even in the final seconds, and the other sets it off.
/// </summary>
public sealed class WFOrdnanceWiresTest
{
    private const string Warhead = "WFWarheadTacticalKeyed";

    /// <summary>Every effect but the blast, so the test map survives.</summary>
    private static WFWarheadPayload QuietPayload()
    {
        return new WFWarheadPayload
        {
            PromptRadiation = 0f,
            FalloutCount = 0,
            EmpRange = 0f,
            DetonationSound = null,
        };
    }

    [Test]
    public async Task TimersStartAtNinetySeconds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var warhead = entMan.SpawnEntity(Warhead, map.GridCoords);
            var beacon = entMan.SpawnEntity("WFOrdnanceBeaconTactical", map.GridCoords);

            Assert.That(entMan.GetComponent<WFTimerFuzeComponent>(warhead).Options.Min(), Is.GreaterThanOrEqualTo(90f));
            Assert.That(entMan.GetComponent<WFRemoteFuzeComponent>(warhead).SignalTimer, Is.GreaterThanOrEqualTo(90f));
            Assert.That(entMan.GetComponent<WFOrdnanceBeaconComponent>(beacon).TimerOptions.Min(), Is.GreaterThanOrEqualTo(90f));
            Assert.That(entMan.GetComponent<WFWarheadComponent>(warhead).DisarmQuality, Is.Null,
                "a multitool still disarms the warhead around its wires.");

            // The panel's nudge buttons can't take it under 90 s either.
            entMan.EventBus.RaiseLocalEvent(warhead, new WFWarheadSetTimerMessage(30f));
            Assert.That(entMan.GetComponent<WFWarheadComponent>(warhead).Timer, Is.EqualTo(90f),
                "the control panel set the timer under 90 s.");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task KlaxonSoundsOnlyWhileArmed()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var warheads = entMan.System<WFWarheadSystem>();

        var uid = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            uid = entMan.SpawnEntity(Warhead, map.GridCoords);
            warheads.StartCountdown((uid, entMan.GetComponent<WFWarheadComponent>(uid)), null);
        });
        await pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
            Assert.That(entMan.GetComponent<WFOrdnanceKlaxonComponent>(uid).Stream, Is.Not.Null, "an armed warhead is silent."));

        await server.WaitPost(() => warheads.Disarm((uid, entMan.GetComponent<WFWarheadComponent>(uid)), null));
        await pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
            Assert.That(entMan.GetComponent<WFOrdnanceKlaxonComponent>(uid).Stream, Is.Null, "a safe warhead kept sounding."));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OneWireDisarmsTheOtherDetonates()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var warheads = entMan.System<WFWarheadSystem>();

        EntityUid good = default, bad = default, safe = default;
        await server.WaitPost(() =>
        {
            var user = entMan.SpawnEntity(null, map.GridCoords);

            // The right wire stops it even in the final seconds.
            good = entMan.SpawnEntity(Warhead, map.GridCoords);
            var goodComp = entMan.GetComponent<WFWarheadComponent>(good);
            warheads.StartCountdown((good, goodComp), null, 5f);
            goodComp.State = WFWarheadState.Final;
            Cut<WFOrdnanceDisarmWireAction>(entMan, good, user);
            Assert.That(goodComp.State, Is.EqualTo(WFWarheadState.Safe), "the disarm wire didn't make it safe.");

            bad = entMan.SpawnEntity(Warhead, map.GridCoords);
            var badComp = entMan.GetComponent<WFWarheadComponent>(bad);
            badComp.CustomPayload = QuietPayload();
            warheads.StartCountdown((bad, badComp), null);
            Cut<WFOrdnanceDetonateWireAction>(entMan, bad, user);

            // Cutting while safe does nothing at all.
            safe = entMan.SpawnEntity(Warhead, map.GridCoords);
            entMan.GetComponent<WFWarheadComponent>(safe).CustomPayload = QuietPayload();
            Cut<WFOrdnanceDetonateWireAction>(entMan, safe, user);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(bad), Is.True, "the detonate wire didn't set it off.");
            Assert.That(entMan.EntityExists(safe), Is.True, "a safe warhead went off when its wire was cut.");
            Assert.That(entMan.GetComponent<WFWarheadComponent>(good).State, Is.EqualTo(WFWarheadState.Safe));
        });

        await pair.CleanReturnAsync();
    }

    private static void Cut<T>(IEntityManager entMan, EntityUid warhead, EntityUid user) where T : IWireAction
    {
        var wires = entMan.GetComponent<WiresComponent>(warhead).WiresList;
        Assert.That(wires, Has.Count.EqualTo(2), "the warhead's panel doesn't hold exactly two wires.");
        var wire = wires.Single(w => w.Action is T);
        wire.Action!.Cut(user, wire);
    }
}
