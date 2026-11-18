namespace Content.Server._WG.ShipAdmin;

/// <summary>
/// On a game rule staff started from Shipmaster: the station and grid it is aimed at. Rules that pick a random
/// station or tile pick these instead, or nothing when the rule can't use them.
/// </summary>
[RegisterComponent]
public sealed partial class WGEventTargetComponent : Component
{
    [DataField]
    public EntityUid Station;

    [DataField]
    public EntityUid Grid;
}
