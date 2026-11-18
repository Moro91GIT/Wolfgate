using System.Linq;
using System.Numerics;
using Content.Server._WG.Encounters.Components;
using Content.Server._WG.NpcCrew.Components;
using Content.Server._WG.ReaverCampaign.Components;

namespace Content.Server._WG.ReaverCampaign.Systems;

/// <summary>Hulls boarders took from the campaign: patrols go after each, at any range, until it is broken or taken back.</summary>
public sealed partial class WGReaverCampaignSystem
{
    /// <summary>How many patrols hunt a hull taken from the campaign.</summary>
    public const int StolenHunters = 2;

    /// <summary>How often a hull still stolen has its hunters made up again.</summary>
    private static readonly TimeSpan StolenRetry = TimeSpan.FromSeconds(30);

    /// <summary>Makes up the hunters of every hull boarders still hold; forgets those destroyed or taken back.</summary>
    private void TickStolen(WGReaverCampaignComponent state)
    {
        var now = _timing.CurTime;
        foreach (var (grid, next) in state.Stolen.ToList())
        {
            if (TerminatingOrDeleted(grid) || !HasComp<WGCrewSeizedHullComponent>(grid))
            {
                state.Stolen.Remove(grid);
                continue;
            }

            if (now < next)
                continue;

            state.Stolen[grid] = now + StolenRetry;
            TryRecover(grid);
        }
    }

    /// <summary>
    /// Turns the nearest free patrols, at any range, on a stolen hull until <see cref="StolenHunters"/> are on it,
    /// launching one from the nearest stronghold when none is free. Returns how many are on it.
    /// </summary>
    public int TryRecover(EntityUid hull)
    {
        if (Campaign is not { } campaign || State(true) is not { Finished: false } || TerminatingOrDeleted(hull)
            || Transform(hull).MapID != _territory.SectorMap || _status.IsDisabled(hull) || _encounters.DockedAtStation(hull))
            return 0;

        var at = _transform.GetWorldPosition(hull);
        var patrols = Patrols();
        var on = patrols.Count(patrol => patrol.Comp1.Recovering && patrol.Comp1.Intercepting == hull);
        var free = new List<(Entity<WGReaverPatrolComponent, WGEncounterComponent> Patrol, float Distance)>();
        foreach (var patrol in patrols)
        {
            if (patrol.Comp1.Recovering || patrol.Comp1.Recalled
                || !patrol.Comp2.Ships.TryGetValue(patrol.Comp1.Lead, out var lead) || TerminatingOrDeleted(lead.Grid)
                || lead.Grid == hull)
                continue;

            free.Add((patrol, Vector2.DistanceSquared(_transform.GetWorldPosition(lead.Grid), at)));
        }

        foreach (var (patrol, _) in free.OrderBy(entry => entry.Distance))
        {
            if (on >= StolenHunters)
                break;

            var resume = patrol.Comp1.Intercepting != null ? patrol.Comp1.Resume.ToList() : RemainingRoute(patrol);
            if (!StartIntercept(campaign, patrol, hull, at, resume))
                continue;

            patrol.Comp1.Recovering = true;
            on++;
        }

        if (on > 0)
            return on;

        var standing = Strongholds();
        if (standing.Count == 0)
            return 0;

        var from = standing.MinBy(stronghold => Vector2.DistanceSquared(Position(stronghold.Comp2, stronghold.Comp1.Lead), at));
        if (!TryLaunchPatrol(from.Owner, out var uid) || !TryComp<WGReaverPatrolComponent>(uid, out var comp)
            || !TryComp<WGEncounterComponent>(uid, out var encounter)
            || !StartIntercept(campaign, (uid, comp, encounter), hull, at, comp.Route.ToList()))
            return 0;

        comp.Recovering = true;
        return 1;
    }
}
