using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._WF.PixelShift;

/// <summary>A mob's own sprite shift and tilt, seen by everyone. Kept until reset; walking doesn't clear it.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WFPixelShiftComponent : Component
{
    /// <summary>Screen-aligned shift in pixels.</summary>
    [DataField, AutoNetworkedField]
    public Vector2i Offset;

    /// <summary>Tilt in degrees; positive leans left.</summary>
    [DataField, AutoNetworkedField]
    public int Tilt;

    /// <summary>Client: the shift added to the sprite this frame.</summary>
    [ViewVariables]
    public Vector2 AppliedOffset;

    /// <summary>Client: the tilt added to the sprite this frame.</summary>
    [ViewVariables]
    public Angle AppliedTilt;

    /// <summary>Client: the sprite offset after the shift was added, to tell whether something has set it since.</summary>
    [ViewVariables]
    public Vector2 WrittenOffset;

    /// <summary>Client: the sprite rotation after the tilt was added.</summary>
    [ViewVariables]
    public Angle WrittenRotation;
}

/// <summary>Which pixel shift key an entity's player is holding. Not networked, like the turn key.</summary>
[RegisterComponent]
public sealed partial class WFPixelShiftHeldComponent : Component
{
    [ViewVariables]
    public WFPixelShiftMode Mode;
}

public enum WFPixelShiftMode : byte
{
    Shift,
    Tilt,
}
