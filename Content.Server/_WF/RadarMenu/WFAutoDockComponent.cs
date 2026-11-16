namespace Content.Server._WF.RadarMenu;

/// <summary>On a helm whose autopilot is flying to a dock: where to dock once it arrives. See <see cref="WFAutoDockSystem"/>.</summary>
[RegisterComponent]
public sealed partial class WFAutoDockComponent : Component
{
    [ViewVariables]
    public EntityUid TargetGrid;

    /// <summary>The dock asked for, or null for any free one that fits.</summary>
    [ViewVariables]
    public EntityUid? Dock;

    /// <summary>The pilot who asked, told how it went.</summary>
    [ViewVariables]
    public EntityUid? Pilot;

    /// <summary>After this a stale request no longer docks.</summary>
    [ViewVariables]
    public TimeSpan Expires;
}
