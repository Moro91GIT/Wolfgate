using System.Diagnostics.CodeAnalysis;
using Content.Server.Wires;
using Content.Shared._WF.AdminTech;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Wires;
using Robust.Server.GameObjects;

namespace Content.Server._WF.AdminTech;

/// <summary>
/// The wire legend: a viewer of a wires panel who holds a tool with <see cref="WFWireLegendComponent"/> is told
/// what each wire does. The answer goes to that viewer alone; the panel's shared state is untouched.
/// </summary>
public sealed partial class WFWireLegendSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WiresComponent, WFWireLegendRequestMessage>(OnRequest);
    }

    private void OnRequest(Entity<WiresComponent> ent, ref WFWireLegendRequestMessage args)
    {
        if (TryGetLegend(ent, args.Actor, out var legend))
            _ui.ServerSendUiMessage(ent.Owner, WiresUiKey.Key, new WFWireLegendMessage(legend), args.Actor);
    }

    /// <summary>What each wire of a panel does, for a viewer holding a legend tool. False for anyone else.</summary>
    public bool TryGetLegend(Entity<WiresComponent> ent, EntityUid viewer, [NotNullWhen(true)] out List<WFWireLegendEntry>? legend)
    {
        legend = null;
        var holds = false;
        foreach (var held in _hands.EnumerateHeld(viewer))
        {
            holds |= HasComp<WFWireLegendComponent>(held);
        }

        if (!holds)
            return false;

        legend = new List<WFWireLegendEntry>();
        foreach (var wire in ent.Comp.WiresList)
        {
            // A wire with no action does nothing; it is left out, as it has no name to show.
            if (wire.Action is BaseWireAction action)
                legend.Add(new WFWireLegendEntry(wire.Color, wire.Letter, Loc.GetString(action.Name)));
        }

        return true;
    }
}
