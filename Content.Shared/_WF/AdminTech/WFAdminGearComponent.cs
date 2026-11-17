using Robust.Shared.GameStates;

namespace Content.Shared._WF.AdminTech;

/// <summary>
/// Administrative equipment: while worn, only the wearer or an admin can take it off, and it says what it is on
/// examine.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WFAdminGearComponent : Component;
