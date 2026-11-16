using Robust.Shared.Input;

namespace Content.Shared._WF.AdminObserver;

/// <summary>The admin observer's keys, listed under Options > Controls.</summary>
[KeyFunctions]
public static class WFAdminObserverKeys
{
    /// <summary>Runs <c>aghost</c>: into the admin observer, or back to the body it left.</summary>
    public static readonly BoundKeyFunction WFToggleAghost = "WFToggleAghost";

    /// <summary>Shows the admin observer to players, or hides it again.</summary>
    public static readonly BoundKeyFunction WFToggleManifest = "WFToggleManifest";

    /// <summary>Opens the observer panel.</summary>
    public static readonly BoundKeyFunction WFOpenObserverPanel = "WFOpenObserverPanel";
}
