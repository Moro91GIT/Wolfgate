using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Shared._WF.AdminTech;

/// <summary>Adds and removes the tags of <see cref="WFGrantTagsComponent"/> as the clothing goes on and comes off.</summary>
public sealed partial class WFGrantTagsSystem : EntitySystem
{
    [Dependency] private TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFGrantTagsComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<WFGrantTagsComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<WFGrantTagsComponent> ent, ref GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(ent, out var clothing) || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        EnsureComp<TagComponent>(args.Equipee);
        foreach (var tag in ent.Comp.Tags)
        {
            if (_tags.HasTag(args.Equipee, tag))
                continue;

            _tags.AddTag(args.Equipee, tag);
            ent.Comp.Given.Add(tag);
        }
    }

    private void OnUnequipped(Entity<WFGrantTagsComponent> ent, ref GotUnequippedEvent args)
    {
        foreach (var tag in ent.Comp.Given)
        {
            _tags.RemoveTag(args.Equipee, tag);
        }

        ent.Comp.Given.Clear();
    }
}
