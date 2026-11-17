using System.Numerics;
using Content.Client._WF.DroneRings;
using Content.Shared._WF.Wilderness;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>Zone edges on the scope, and a skull by every ship marked for staying in a Wilderness.</summary>
public partial class ShuttleNavControl
{
    private static readonly Color WfMarkedColor = Color.FromHex("#ff3b30");
    private static readonly SpriteSpecifier WfSkullSprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/_WF/Encounters/markers.rsi"), "skull");

    private readonly List<float> _wfZoneRadii = new() { 0f };
    private readonly List<string> _wfZoneLabels = new() { string.Empty };
    private readonly List<Color> _wfZoneColors = new() { Color.White };
    private Texture? _wfSkull;

    /// <summary>Each zone's edge as a dashed ring in its colour, named where it comes nearest the middle of the scope.</summary>
    private void DrawWfZoneRings(DrawingHandleScreen handle, Matrix3x2 worldToView, EntityUid? map)
    {
        if (!_wfSettingsCfg.GetCVar(WildernessCVars.RadarRings)
            || !EntManager.TryGetComponent<WFZoneRingsComponent>(map, out var rings)
            || rings.Rings.Count == 0)
            return;

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        foreach (var ring in rings.Rings)
        {
            if (!prototypes.TryIndex(ring.Zone, out var zone))
                continue;

            _wfZoneRadii[0] = ring.Radius;
            _wfZoneLabels[0] = Loc.GetString(zone.RingLabel);
            _wfZoneColors[0] = zone.Color;
            WFDroneRingDrawing.Draw(handle, Font, _wfZoneRadii, Vector2.Transform(ring.Centre, worldToView), MinimapScale, PixelSize, UIScale,
                _wfZoneLabels, _wfZoneColors);
        }
    }

    /// <summary>A marked ship's name in red.</summary>
    private Color WfMarkColor(EntityUid grid, Color color)
    {
        return EntManager.HasComponent<WFSkullComponent>(grid) ? WfMarkedColor : color;
    }

    /// <summary>
    /// For a marked ship: a skull after its name in the current tag, and one over its blip at <paramref name="blip"/>
    /// whether its name shows or not.
    /// </summary>
    private void WfTagSkull(DrawingHandleScreen handle, EntityUid grid, Vector2 blip, string name)
    {
        if (!EntManager.HasComponent<WFSkullComponent>(grid))
            return;

        _wfSkull ??= EntManager.System<SpriteSystem>().Frame0(WfSkullSprite);
        var skull = _wfSkull;
        var line = handle.GetDimensions(Font, name, UIScale * 0.9f).Y;
        WfTagHeadExtra(line + 3f * UIScale,
            (h, at) => h.DrawTextureRect(skull, UIBox2.FromDimensions(at + new Vector2(3f * UIScale, 0f), new Vector2(line)), WfMarkedColor));

        var size = 12f * UIScale;
        handle.DrawTextureRect(skull, UIBox2.FromDimensions(blip + new Vector2(-size / 2f, -size - 6f * UIScale), new Vector2(size)), WfMarkedColor);
    }
}
