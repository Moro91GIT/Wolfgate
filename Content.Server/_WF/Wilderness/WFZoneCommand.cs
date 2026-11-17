using Content.Server.Administration;
using Content.Shared._WF.Wilderness;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server._WF.Wilderness;

/// <summary>Shows the zone and Wilderness mark of the admin's own ship or person, and sets or clears the mark.</summary>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class WFZoneCommand : LocalizedEntityCommands
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WFZoneSystem _zones = default!;

    public override string Command => "wf_zone";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } mob)
        {
            shell.WriteError(Loc.GetString("cmd-wf_zone-no-body"));
            return;
        }

        var carrier = _zones.CarrierOf(mob);
        switch (args.Length == 0 ? "info" : args[0])
        {
            case "info":
                var zone = EntityManager.TryGetComponent<WFInZoneComponent>(mob, out var inZone) ? inZone.Zone.Id : "-";
                var mark = EntityManager.TryGetComponent<WFSkullComponent>(carrier, out var skull)
                    ? skull.ExpiresAt is { } at ? WFZoneText.Clock(at - _timing.CurTime) : Loc.GetString("cmd-wf_zone-held")
                    : "-";
                shell.WriteLine(Loc.GetString("cmd-wf_zone-info", ("zone", zone), ("carrier", EntityManager.ToPrettyString(carrier)), ("mark", mark)));
                break;
            case "mark":
                _zones.Mark(carrier);
                shell.WriteLine(Loc.GetString("cmd-wf_zone-marked", ("carrier", EntityManager.ToPrettyString(carrier))));
                break;
            case "clear":
                EntityManager.RemoveComponent<WFSkullComponent>(carrier);
                EntityManager.RemoveComponent<WFWildernessDwellComponent>(carrier);
                shell.WriteLine(Loc.GetString("cmd-wf_zone-cleared", ("carrier", EntityManager.ToPrettyString(carrier))));
                break;
            default:
                shell.WriteLine(Help);
                break;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(new[] { "info", "mark", "clear" }, Loc.GetString("cmd-wf_zone-hint"))
            : CompletionResult.Empty;
    }
}
