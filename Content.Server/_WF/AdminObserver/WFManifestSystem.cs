using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Eye;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._WF.AdminObserver;

/// <summary>
/// Manifesting: an admin observer becomes visible to living players as an avatar, takes an in-character name,
/// speaks as the living do and carries the staff marker; hiding undoes each step. It stays a ghost throughout, so
/// it keeps every observer power and nothing can touch it.
/// </summary>
public sealed partial class WFManifestSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    [Dependency] private WFAdminObserverStore _store = default!;
    [Dependency] private WFAvatarBodySystem _body = default!;
    [Dependency] private WFStaffMarkerSystem _marker = default!;

    private static readonly EntProtoId BluespaceEffect = "EffectFlashBluespace";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFAdminObserverComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WFAdminObserverComponent, WFToggleManifestEvent>(OnToggle);
    }

    private void OnMapInit(Entity<WFAdminObserverComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ManifestActionEntity, ent.Comp.ManifestAction);
    }

    private void OnToggle(Entity<WFAdminObserverComponent> ent, ref WFToggleManifestEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        var done = HasComp<WFManifestedComponent>(ent)
            ? TryUnmanifest(actor.PlayerSession, out var message)
            : TryManifest(actor.PlayerSession, null, out message);

        _popup.PopupCursor(message, actor.PlayerSession, done ? PopupType.Small : PopupType.SmallCaution);
    }

    /// <summary>
    /// Shows a player's admin observer to the living as one of their presets: the named one, else their default,
    /// else the plain ghost. <paramref name="message"/> says what happened, in the player's words.
    /// </summary>
    public bool TryManifest(ICommonSession player, string? presetName, out string message)
    {
        if (player.AttachedEntity is not { } uid || !HasComp<WFAdminObserverComponent>(uid))
        {
            message = Loc.GetString("wf-observer-manifest-not-observer");
            return false;
        }

        if (_store.Get(player) is not { } data)
        {
            message = Loc.GetString("wf-observer-manifest-no-permission");
            return false;
        }

        presetName ??= data.DefaultPreset;
        var preset = presetName == null ? new WFAvatarPreset() : data.Presets.FirstOrDefault(p => p.Name == presetName);
        if (preset == null)
        {
            message = Loc.GetString("wf-observer-manifest-unknown-preset", ("name", presetName!));
            return false;
        }

        if (preset.Kind is WFAvatarKind.Object or WFAvatarKind.Mob && !IsAvatarPrototype(preset.Prototype))
        {
            message = Loc.GetString("wf-observer-preset-prototype-invalid", ("id", preset.Prototype ?? string.Empty));
            return false;
        }

        var character = _mind.TryGetMind(player, out _, out var mind) ? mind.CharacterName : null;
        if (character == player.Name)
            character = null;

        EntityUid? body = null;
        if (preset.Kind == WFAvatarKind.Character)
        {
            body = _body.Ensure((uid, Comp<WFAdminObserverComponent>(uid)), player, preset);
            if (body == null)
            {
                message = Loc.GetString("wf-observer-preset-no-characters");
                return false;
            }

            character = _body.Profile(player, preset)?.Name ?? character;
        }

        Unmanifest(uid, effects: false);
        Manifest(uid, preset, character, body);
        _marker.Mark(uid, player, data.Marker, preset.Kind);

        message = Loc.GetString("wf-observer-manifest-done", ("name", Name(uid)));
        return true;
    }

    /// <summary>
    /// Whether an entity prototype can be shown as an object or mob avatar: it exists and can be spawned. Whether
    /// it has a sprite is the client's to check, as the server reads none.
    /// </summary>
    public bool IsAvatarPrototype(string? id)
    {
        return id != null && _prototypes.TryIndex<EntityPrototype>(id, out var prototype) && !prototype.Abstract;
    }

    /// <summary>Hides a player's admin observer from the living again.</summary>
    public bool TryUnmanifest(ICommonSession player, out string message)
    {
        if (player.AttachedEntity is not { } uid || !HasComp<WFAdminObserverComponent>(uid))
        {
            message = Loc.GetString("wf-observer-manifest-not-observer");
            return false;
        }

        if (!HasComp<WFManifestedComponent>(uid))
        {
            message = Loc.GetString("wf-observer-manifest-not-manifested");
            return false;
        }

        Unmanifest(uid);
        message = Loc.GetString("wf-observer-manifest-hidden");
        return true;
    }

    /// <summary>
    /// Makes an entity visible as a preset. The caller marks it as staff straight after: an observer is never
    /// visible unmarked, so until then it carries a marker that says only "Staff".
    /// </summary>
    public void Manifest(EntityUid uid, WFAvatarPreset preset, string? characterName, EntityUid? body = null)
    {
        var comp = EnsureComp<WFManifestedComponent>(uid);
        var meta = MetaData(uid);
        comp.PreviousName = meta.EntityName;
        comp.PreviousDescription = meta.EntityDescription;
        comp.Kind = preset.Kind;
        comp.Prototype = preset.Prototype;
        comp.Body = body;
        comp.EntryStyle = preset.EntryStyle;
        comp.Preset = preset.Name;

        EntityPrototype? avatar = null;
        if (preset.Prototype != null)
            _prototypes.TryIndex(preset.Prototype, out avatar);

        _metaData.SetEntityName(uid, AvatarName(uid, preset, avatar, characterName), meta);
        if (preset.Description.Length > 0)
            _metaData.SetEntityDescription(uid, preset.Description, meta);
        else if (avatar != null)
            _metaData.SetEntityDescription(uid, avatar.Description, meta);
        else if (body != null)
            _metaData.SetEntityDescription(uid, MetaData(body.Value).EntityDescription, meta);

        // A character avatar sounds like its character.
        if (body != null && TryComp<SpeechComponent>(body, out var voice) && TryComp<SpeechComponent>(uid, out var speech))
        {
            comp.ChangedVoice = true;
            comp.PreviousSpeechSounds = speech.SpeechSounds;
            comp.PreviousSpeechVerb = speech.SpeechVerb;
            speech.SpeechSounds = voice.SpeechSounds;
            speech.SpeechVerb = voice.SpeechVerb;
        }

        _marker.Mark(uid, new WFStaffMarkerSettings { Wording = WFMarkerWording.Staff }, null, string.Empty, false, preset.Kind);
        SetVisible(uid, true);

        if (!HasComp<IntrinsicRadioTransmitterComponent>(uid))
        {
            var transmitter = AddComp<IntrinsicRadioTransmitterComponent>(uid);
            transmitter.Channels = _prototypes.EnumeratePrototypes<RadioChannelPrototype>()
                .Select(c => new ProtoId<RadioChannelPrototype>(c.ID))
                .ToHashSet();
            comp.AddedTransmitter = true;
        }

        // Staff radio must not depend on a telecom server standing on the same map.
        if (!HasComp<TelecomExemptComponent>(uid))
        {
            AddComp<TelecomExemptComponent>(uid);
            comp.AddedTelecomExempt = true;
        }

        Dirty(uid, comp);
        PlayEntry(uid, comp.EntryStyle);
        if (TryComp<WFAdminObserverComponent>(uid, out var observer))
            _actions.SetToggled(observer.ManifestActionEntity, true);

        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{ToPrettyString(uid):player} manifested as \"{Name(uid)}\" ({preset.Kind}, preset \"{preset.Name}\") at {Transform(uid).Coordinates}");
    }

    /// <summary>Hides a manifested entity and gives it back what manifesting changed. Does nothing to any other.</summary>
    public void Unmanifest(EntityUid uid, bool effects = true)
    {
        if (!TryComp<WFManifestedComponent>(uid, out var comp))
            return;

        if (effects)
            PlayEntry(uid, comp.EntryStyle);

        var meta = MetaData(uid);
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{ToPrettyString(uid):player} stopped being manifested as \"{meta.EntityName}\" at {Transform(uid).Coordinates}");

        _metaData.SetEntityName(uid, comp.PreviousName, meta);
        _metaData.SetEntityDescription(uid, comp.PreviousDescription, meta);
        SetVisible(uid, false);

        if (comp.AddedTransmitter)
            RemComp<IntrinsicRadioTransmitterComponent>(uid);

        if (comp.AddedTelecomExempt)
            RemComp<TelecomExemptComponent>(uid);

        if (comp.ChangedVoice && TryComp<SpeechComponent>(uid, out var speech))
        {
            speech.SpeechSounds = comp.PreviousSpeechSounds;
            speech.SpeechVerb = comp.PreviousSpeechVerb;
        }

        RemComp<WFStaffMarkerComponent>(uid);
        RemComp<WFManifestedComponent>(uid);

        if (TryComp<WFAdminObserverComponent>(uid, out var observer))
            _actions.SetToggled(observer.ManifestActionEntity, false);
    }

    /// <summary>
    /// The name players see: the preset's, else the avatar prototype's, else the character the admin plays, else
    /// the observer prototype's. Never the account name.
    /// </summary>
    private string AvatarName(EntityUid uid, WFAvatarPreset preset, EntityPrototype? avatar, string? characterName)
    {
        if (!string.IsNullOrWhiteSpace(preset.DisplayName))
            return preset.DisplayName;

        if (avatar != null)
            return avatar.Name;

        if (!string.IsNullOrWhiteSpace(characterName))
            return characterName;

        return Prototype(uid)?.Name ?? Name(uid);
    }

    /// <summary>Moves an entity between the layer only ghosts see and the one everyone sees.</summary>
    private void SetVisible(EntityUid uid, bool visible)
    {
        var visibility = EnsureComp<VisibilityComponent>(uid);
        var shown = (ushort) (visible ? VisibilityFlags.Normal : VisibilityFlags.Ghost);
        var hidden = (ushort) (visible ? VisibilityFlags.Ghost : VisibilityFlags.Normal);
        _visibility.AddLayer((uid, visibility), shown, false);
        _visibility.RemoveLayer((uid, visibility), hidden, false);
        _visibility.RefreshVisibility(uid, visibility);
    }

    private void PlayEntry(EntityUid uid, WFEntryStyle style)
    {
        if (style == WFEntryStyle.Bluespace)
            Spawn(BluespaceEffect, Transform(uid).Coordinates);
    }
}
