using Robust.Shared.Configuration;

namespace Content.Shared._WG.Wilderness;

/// <summary>Rules and display settings for zones and the Wilderness mark.</summary>
[CVarDefs]
public sealed class WildernessCVars
{
    /// <summary>Whether anyone may fight anyone in a Wilderness, and engage a marked ship, without escalation. Off: TSF and PDV only.</summary>
    public static readonly CVarDef<bool> OpenToAll =
        CVarDef.Create("wg.wilderness.open_to_all", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>Whether someone not aboard a ship is marked themselves. Off: only ships are marked.</summary>
    public static readonly CVarDef<bool> SkullOnFoot =
        CVarDef.Create("wg.wilderness.skull_on_foot", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>Seconds in a Wilderness before a ship or person is marked.</summary>
    public static readonly CVarDef<float> DwellSeconds =
        CVarDef.Create("wg.wilderness.dwell_seconds", 15f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>Minutes the mark lasts after leaving a Wilderness.</summary>
    public static readonly CVarDef<float> SkullMinutes =
        CVarDef.Create("wg.wilderness.skull_minutes", 10f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>Whether the coloured screen border shows in a Wilderness and while marked.</summary>
    public static readonly CVarDef<bool> Border =
        CVarDef.Create("wg.wilderness.border", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>Whether radars draw zone edges.</summary>
    public static readonly CVarDef<bool> RadarRings =
        CVarDef.Create("wg.wilderness.radar_rings", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
