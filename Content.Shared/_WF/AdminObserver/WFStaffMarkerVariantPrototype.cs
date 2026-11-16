using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._WF.AdminObserver;

/// <summary>One look a staff marker visual can take, such as one of the ground rings.</summary>
[Prototype("wfStaffMarkerVariant")]
public sealed partial class WFStaffMarkerVariantPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>The single visual this is a look for.</summary>
    [DataField(required: true)]
    public WFMarkerVisuals Visual;

    /// <summary>How the look is drawn.</summary>
    [DataField]
    public WFMarkerVariantKind Kind = WFMarkerVariantKind.Sprite;

    /// <summary>The sprite drawn, for <see cref="WFMarkerVariantKind.Sprite"/>. Used where it is, in its own RSI.</summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    /// <summary>Plays the sprite's frames last to first.</summary>
    [DataField]
    public bool Reversed;

    /// <summary>Colours the sprite's whole shape, for a sprite too dark to tint.</summary>
    [DataField]
    public bool Solid;

    /// <summary>How wide the sprite is drawn, in tiles.</summary>
    [DataField]
    public float Size = 1f;

    /// <summary>Whether a glyph circles the avatar; otherwise it sits over it.</summary>
    [DataField]
    public bool Orbit = true;

    /// <summary>The look used when the admin has chosen none, or one that no longer exists. One per visual.</summary>
    [DataField]
    public bool Default;

    /// <summary>The look's place in the admin's list.</summary>
    [DataField]
    public int Order;
}

/// <summary>How a staff marker look is drawn.</summary>
public enum WFMarkerVariantKind : byte
{
    /// <summary>A sprite tinted in the marker colour.</summary>
    Sprite,

    /// <summary>A shader: the shield skin for a shield, an outline for an aura.</summary>
    Shader,

    /// <summary>A point light in the marker colour.</summary>
    Light,
}
