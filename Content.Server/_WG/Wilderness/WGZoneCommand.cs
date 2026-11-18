using Content.Server.Administration;
using Content.Shared._WG.Wilderness;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server._WG.Wilderness;

/// <summary>Shows the zone and Wilderness mark of the admin's own ship or person, and sets or clears the mark.</summary>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class WGZoneCommand : LocalizedEntityCommands
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private WGZoneSystem _zones = default!;

    public override string Command => "wg_zone";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } mob)
        {
            shell.WriteError(Loc.GetString("cmd-wg_zone-no-body"));
            return;
        }

        var carrier = _zones.CarrierOf(mob);
        switch (args.Length == 0 ? "info" : args[0])
        {
            case "info":
                var zone = EntityManager.TryGetComponent<WGInZoneComponent>(mob, out var inZone) ? inZone.Zone.Id : "-";
                var mark = EntityManager.TryGetComponent<WGSkullComponent>(carrier, out var skull)
                    ? skull.ExpiresAt is { } at ? WGZoneText.Clock(at - _timing.CurTime) : Loc.GetString("cmd-wg_zone-held")
                    : "-";
                shell.WriteLine(Loc.GetString("cmd-wg_zone-info", ("zone", zone), ("carrier", EntityManager.ToPrettyString(carrier)), ("mark", mark)));
                break;
            case "mark":
                _zones.Mark(carrier);
                shell.WriteLine(Loc.GetString("cmd-wg_zone-marked", ("carrier", EntityManager.ToPrettyString(carrier))));
                break;
            case "clear":
                EntityManager.RemoveComponent<WGSkullComponent>(carrier);
                EntityManager.RemoveComponent<WGWildernessDwellComponent>(carrier);
                shell.WriteLine(Loc.GetString("cmd-wg_zone-cleared", ("carrier", EntityManager.ToPrettyString(carrier))));
                break;
            default:
                shell.WriteLine(Help);
                break;
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(new[] { "info", "mark", "clear" }, Loc.GetString("cmd-wg_zone-hint"))
            : CompletionResult.Empty;
    }
}
