using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._WF.AdminObserver;

/// <summary>Marks the admin observer and tracks whose kit it carries. The fields are the server's.</summary>
[RegisterComponent]
public sealed partial class WFAdminObserverComponent : Component
{
    /// <summary>The player whose kit this observer was given.</summary>
    [ViewVariables]
    public NetUserId? Owner;

    /// <summary>Whether the kit has been handed out; it is once per observer.</summary>
    [ViewVariables]
    public bool KitApplied;

    /// <summary>Whether what this observer carries is written back to <see cref="Owner"/>'s saved layout.</summary>
    [ViewVariables]
    public bool SavesKit;

    /// <summary>Whether items outside the kit catalogue are saved too: the owner holds the Spawn flag.</summary>
    [ViewVariables]
    public bool SavesAnything;

    /// <summary>Whether the layout was saved when the player last left, so deletion need not save it again.</summary>
    [ViewVariables]
    public bool SavedOnLeave;

    /// <summary>The action that shows the observer to players and hides it again.</summary>
    [DataField]
    public EntProtoId ManifestAction = "WFActionManifest";

    [ViewVariables]
    public EntityUid? ManifestActionEntity;
}
