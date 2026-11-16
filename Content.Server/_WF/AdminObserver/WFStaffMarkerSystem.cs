using Content.Server.Administration.Managers;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._WF.AdminObserver;

/// <summary>
/// The staff marker's fixed part: whoever carries <see cref="WFStaffMarkerComponent"/> says so on examine and in
/// front of their name in chat. Also fills the marker from an admin's saved choices and their flags.
/// </summary>
public sealed partial class WFStaffMarkerSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    /// <summary>The visuals the client can draw. A marker with none of them gets the nameplate.</summary>
    public const WFMarkerVisuals Supported = WFMarkerVisuals.Nameplate | WFMarkerVisuals.Sigil | WFMarkerVisuals.Aura
                                             | WFMarkerVisuals.Shield | WFMarkerVisuals.Hologram
                                             | WFMarkerVisuals.Glyph | WFMarkerVisuals.Ring;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFStaffMarkerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WFStaffMarkerComponent, TransformSpeakerNameEvent>(OnSpeakerName);
    }

    private void OnExamined(Entity<WFStaffMarkerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("wf-staff-marker-examine",
            ("ent", ent.Owner),
            ("label", FormattedMessage.EscapeText(ent.Comp.Label))));
    }

    private void OnSpeakerName(Entity<WFStaffMarkerComponent> ent, ref TransformSpeakerNameEvent args)
    {
        args.VoiceName = Loc.GetString("wf-staff-marker-name", ("label", ent.Comp.Label), ("name", args.VoiceName));
    }

    /// <summary>The visuals drawn on the avatar itself. A nameplate and a sigil sit beside it.</summary>
    public const WFMarkerVisuals OnAvatar = WFMarkerVisuals.Aura | WFMarkerVisuals.Shield | WFMarkerVisuals.Hologram
                                            | WFMarkerVisuals.Glyph | WFMarkerVisuals.Ring;

    /// <summary>What an object or mob avatar gets when the admin chose nothing drawn on the avatar itself.</summary>
    public const WFMarkerVisuals ObjectDefault = WFMarkerVisuals.Hologram | WFMarkerVisuals.Ring;

    /// <summary>Marks an entity as this admin, by their saved choices, rank and flags.</summary>
    public WFStaffMarkerComponent Mark(
        EntityUid uid,
        ICommonSession admin,
        WFStaffMarkerSettings settings,
        WFAvatarKind kind = WFAvatarKind.Ghost)
    {
        return Mark(uid, settings, _admin.GetAdminData(admin)?.Title, admin.Name, _admin.HasAdminFlag(admin, AdminFlags.Host), kind);
    }

    /// <summary>
    /// Marks an entity as staff. The colour and sigil follow <paramref name="host"/>, not the saved settings: only
    /// a holder of the Host flag keeps a colour of their own.
    /// </summary>
    public WFStaffMarkerComponent Mark(
        EntityUid uid,
        WFStaffMarkerSettings settings,
        string? rank,
        string account,
        bool host,
        WFAvatarKind kind = WFAvatarKind.Ghost)
    {
        var marker = EnsureComp<WFStaffMarkerComponent>(uid);
        marker.Label = Label(settings.Wording, rank, account);
        marker.Visuals = VisualsFor(kind, settings.Visuals);
        marker.Host = host;
        marker.Color = MarkerColor(settings.Color, host);
        marker.Ring = Variant(WFMarkerVisuals.Ring, settings.Ring);
        marker.Glyph = Variant(WFMarkerVisuals.Glyph, settings.Glyph);
        marker.Shield = Variant(WFMarkerVisuals.Shield, settings.Shield);
        marker.Aura = Variant(WFMarkerVisuals.Aura, settings.Aura);
        Dirty(uid, marker);
        return marker;
    }

    /// <summary>
    /// The visuals an avatar is marked with: the chosen ones, never none. An object or mob could pass for the real
    /// thing with a plate beside it and nothing else, so it also gets <see cref="ObjectDefault"/> unless the admin
    /// chose something drawn on the avatar.
    /// </summary>
    public static WFMarkerVisuals VisualsFor(WFAvatarKind kind, WFMarkerVisuals chosen)
    {
        var visuals = chosen & Supported;
        if (kind is WFAvatarKind.Object or WFAvatarKind.Mob && (visuals & OnAvatar) == 0)
            visuals |= ObjectDefault;

        return visuals == WFMarkerVisuals.None ? WFMarkerVisuals.Nameplate : visuals;
    }

    /// <summary>The look a visual takes: the chosen one if it exists and is a look for that visual, else the default.</summary>
    public string Variant(WFMarkerVisuals visual, string chosen)
    {
        if (_prototypes.TryIndex<WFStaffMarkerVariantPrototype>(chosen, out var variant) && variant.Visual == visual)
            return variant.ID;

        foreach (var candidate in _prototypes.EnumeratePrototypes<WFStaffMarkerVariantPrototype>())
        {
            if (candidate.Visual == visual && candidate.Default)
                return candidate.ID;
        }

        return string.Empty;
    }

    /// <summary>The staff wording for an admin. A missing rank reads as "Staff".</summary>
    public string Label(WFMarkerWording wording, string? rank, string account)
    {
        var staff = Loc.GetString("wf-staff-marker-label-staff");
        rank = string.IsNullOrWhiteSpace(rank) ? staff : rank;

        return wording switch
        {
            WFMarkerWording.Staff => staff,
            WFMarkerWording.Name => account,
            WFMarkerWording.RankAndName => Loc.GetString("wf-staff-marker-label-rank-and-name", ("rank", rank), ("name", account)),
            _ => rank,
        };
    }

    /// <summary>The marker colour: fixed for staff, the saved one for a holder of the Host flag.</summary>
    public static Color MarkerColor(string? saved, bool host)
    {
        if (!host)
            return WFStaffMarkerComponent.StaffColor;

        return saved != null && Color.TryFromHex(saved, out var color) ? color : WFStaffMarkerComponent.HostColor;
    }
}
