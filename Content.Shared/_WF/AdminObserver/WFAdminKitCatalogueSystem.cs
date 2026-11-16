using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.AdminObserver;

/// <summary>Answers what is in the admin observer's kit catalogue (<see cref="WFAdminKitCategoryPrototype"/>).</summary>
public sealed partial class WFAdminKitCatalogueSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly HashSet<string> _entries = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        Rebuild();
    }

    /// <summary>Whether an entity prototype is a kit item.</summary>
    public bool Contains(string id)
    {
        return _entries.Contains(id);
    }

    /// <summary>The catalogue's groups in display order.</summary>
    public List<WFAdminKitCategoryPrototype> Categories()
    {
        return _prototypes.EnumeratePrototypes<WFAdminKitCategoryPrototype>()
            .OrderBy(c => c.Order)
            .ThenBy(c => c.ID)
            .ToList();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<WFAdminKitCategoryPrototype>())
            Rebuild();
    }

    private void Rebuild()
    {
        _entries.Clear();
        foreach (var category in _prototypes.EnumeratePrototypes<WFAdminKitCategoryPrototype>())
        {
            foreach (var entry in category.Entries)
            {
                _entries.Add(entry.Id);
            }
        }
    }
}
