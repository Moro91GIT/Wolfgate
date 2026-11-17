using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;

namespace Content.Shared._WF.AdminTech;

/// <summary>Keeps administrative gear on its wearer and labels it on examine.</summary>
public sealed partial class WFAdminGearSystem : EntitySystem
{
    [Dependency] private ISharedAdminManager _admin = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFAdminGearComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<WFAdminGearComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUnequipAttempt(Entity<WFAdminGearComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (args.Cancelled || args.Unequipee == args.UnEquipTarget || _admin.HasAdminFlag(args.Unequipee, AdminFlags.Admin))
            return;

        args.Reason = "wf-admin-gear-strip-blocked";
        args.Cancel();
    }

    private void OnExamined(Entity<WFAdminGearComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("wf-admin-gear-examine"));
    }
}
