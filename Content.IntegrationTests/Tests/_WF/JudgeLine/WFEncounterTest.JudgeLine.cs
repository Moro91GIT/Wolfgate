#nullable enable
using System.Linq;
using System.Numerics;
using Content.Server._WF.Encounters.Components;
using Content.Server._WF.Encounters.Systems;
using Content.Server._WF.JudgeLine;
using Content.Server._WF.NpcCrew.Systems;
using Content.Server.Shuttles.Components;
using Content.Shared._WF.Encounters;
using Content.Shared._WF.JudgeLine;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._WF.Encounters;

/// <summary>
/// The Judge Line: a call brings a judge responder to the caller, which deals with the real emergency, the chosen one or
/// not, and counts a call with none as a hoax.
/// </summary>
public sealed partial class WFEncounterTest
{
    /// <summary>Runs a test with the responder dispatched at once and dropping out of FTL close by.</summary>
    private async Task WithQuickJudges(Func<Task> test)
    {
        var config = Server.ResolveDependency<IConfigurationManager>();
        await Server.WaitPost(() =>
        {
            config.SetCVar(JudgeLineCVars.DispatchDelay, 0f);
            config.SetCVar(JudgeLineCVars.ArrivalDistance, 400f);
        });
        try
        {
            await test();
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                var line = Server.System<WFJudgeLineSystem>();
                foreach (var call in line.Calls.Where(c => c.Caller == SPlayer).ToList())
                {
                    line.Cancel(call);
                    if (call.Encounter is { } encounter && SEntMan.EntityExists(encounter))
                        Server.System<WFEncounterSystem>().End(encounter);
                }

                config.SetCVar(JudgeLineCVars.DispatchDelay, JudgeLineCVars.DispatchDelay.DefaultValue);
                config.SetCVar(JudgeLineCVars.ArrivalDistance, JudgeLineCVars.ArrivalDistance.DefaultValue);
            });
        }
    }

    /// <summary>
    /// A medical call from a ship that is really under fire: the responder takes the fight instead, allies with the
    /// caller's ship and turns on the ship that hit it.
    /// </summary>
    [Test]
    public async Task JudgeLineAnswersTheRealEmergencyAndFightsTheAttacker()
    {
        await WithQuickJudges(async () =>
        {
            var line = Server.System<WFJudgeLineSystem>();
            var alerts = Server.System<WFCrewAlertSystem>();
            var escorts = Server.System<WFCrewEscortSystem>();
            var at = new Vector2(-90000, -90000);
            WFJudgeLineCall call = default!;
            EntityUid caller = default, raider = default;
            await Server.WaitAssertion(() =>
            {
                caller = ClampDeck(at).Grid;
                raider = ClampDeck(at + new Vector2(150, 0)).Grid;
                SEntMan.GetComponent<ShuttleComponent>(caller).LinearThrust[0] = 1000f;
                Server.System<SharedTransformSystem>().SetCoordinates(SPlayer, new EntityCoordinates(caller, new Vector2(1.5f, 0.5f)));
                line.RecordHit(caller, raider);
                call = line.Place(SPlayer, WFJudgeLineKind.Medical);
                Assert.That(call.RealAtCall, Is.True, "A hit ship is a real emergency.");
            });

            await RunTicks(Ticks(4));
            await Server.WaitAssertion(() =>
            {
                var ship = line.Ship(call);
                Assert.That(ship, Is.Not.Null, $"The responder is out (phase {call.Phase}).");
                Assert.That(call.Arrived, Is.Not.Null, "It has arrived.");
                Assert.That(call.Kind, Is.EqualTo(WFJudgeLineKind.UnderFire), "It deals with the real emergency.");
                Assert.That(escorts.AreInFormation(ship!.Grid, caller), Is.True, "It sides with the caller's ship.");
                Assert.That(alerts.IsHostileShip(ship.Grid, ship.Group, raider), Is.True, "It takes the attacker for hostile.");
                Server.System<WFEncounterSystem>().End(call.Encounter!.Value);
            });
        });
    }

    /// <summary>A call with no emergency, then or on arrival, is a hoax: a strike, and the responder leaves.</summary>
    [Test]
    public async Task JudgeLineCountsAHoax()
    {
        await WithQuickJudges(async () =>
        {
            var line = Server.System<WFJudgeLineSystem>();
            var at = new Vector2(-90000, 90000);
            WFJudgeLineCall call = default!;
            var strikes = 0;
            await Server.WaitAssertion(() =>
            {
                Assert.That(line.Refusal(SPlayer), Is.Not.Null, "Someone with no job is no vagrant.");
                strikes = line.Strikes(SEntMan.GetComponent<ActorComponent>(SPlayer).PlayerSession.UserId);
                var caller = ClampDeck(at).Grid;
                SEntMan.GetComponent<ShuttleComponent>(caller).LinearThrust[0] = 1000f;
                Server.System<SharedTransformSystem>().SetCoordinates(SPlayer, new EntityCoordinates(caller, new Vector2(1.5f, 0.5f)));
                call = line.Place(SPlayer, WFJudgeLineKind.Boarded);
                Assert.That(call.RealAtCall, Is.False);
            });

            await RunTicks(Ticks(4));
            await Server.WaitAssertion(() =>
            {
                Assert.That(call.Phase, Is.EqualTo(WFJudgeLinePhase.Leaving), "The responder leaves.");
                var user = SEntMan.GetComponent<ActorComponent>(SPlayer).PlayerSession.UserId;
                Assert.That(line.Strikes(user), Is.EqualTo(strikes + 1), "The hoax counts against the caller.");
                Assert.That(line.CallOf(SPlayer), Is.Null);
                Assert.That(SEntMan.GetComponent<WFEncounterComponent>(call.Encounter!.Value).Resolution,
                    Is.EqualTo(WFEncounterResolution.Completed));
                Server.System<WFEncounterSystem>().End(call.Encounter!.Value);
            });
        });
    }
}
