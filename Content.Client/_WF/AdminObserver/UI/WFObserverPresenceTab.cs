using System.Linq;
using System.Numerics;
using Content.Client._WF.Administration.UI.SpawnOutfit;
using Content.Client.Administration.Managers;
using Content.Client.Lobby;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._WF.AdminObserver.UI;

/// <summary>
/// The Presence tab: the manifest toggle, the avatar presets and their editor, and the staff marker. It shows the
/// admin's saved document and sends each change as it is made; what comes back from the server redraws it.
/// </summary>
public sealed class WFObserverPresenceTab : BoxContainer
{
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly WFAdminObserverClientSystem _data;
    private readonly WFManifestClientSystem _manifest;
    private readonly WFObserverPanelClientSystem _panel;
    private readonly SpriteSystem _sprite;

    private static readonly WFMarkerVisuals[] Visuals =
    {
        WFMarkerVisuals.Nameplate, WFMarkerVisuals.Sigil, WFMarkerVisuals.Aura, WFMarkerVisuals.Shield,
        WFMarkerVisuals.Hologram, WFMarkerVisuals.Glyph, WFMarkerVisuals.Ring,
    };

    private static readonly WFMarkerWording[] Wordings =
    {
        WFMarkerWording.Rank, WFMarkerWording.Staff, WFMarkerWording.Name, WFMarkerWording.RankAndName,
    };

    private static readonly WFAvatarKind[] Kinds =
    {
        WFAvatarKind.Ghost, WFAvatarKind.Object, WFAvatarKind.Mob, WFAvatarKind.Character,
    };

    private readonly Label _state = new() { HorizontalExpand = true };
    private readonly Button _manifestButton = new();
    private readonly ItemList _presets = new() { MinSize = new Vector2(0, 130), HorizontalExpand = true, SelectMode = ItemList.ItemListSelectMode.Single };
    private readonly Button _edit = new() { Text = Loc.GetString("wf-observer-preset-edit"), ToggleMode = true };
    private readonly Button _makeDefault = new() { Text = Loc.GetString("wf-observer-preset-set-default") };
    private readonly Button _delete = new() { Text = Loc.GetString("wf-observer-preset-delete") };

    private readonly BoxContainer _editor = Column();
    private readonly OptionButton _kind = new();
    private readonly LineEdit _name = new() { PlaceHolder = Loc.GetString("wf-observer-preset-name-placeholder") };
    private readonly LineEdit _description = new() { PlaceHolder = Loc.GetString("wf-observer-preset-description-placeholder") };
    private readonly BoxContainer _prototypeRow = Row();
    private readonly Label _prototype = new() { HorizontalExpand = true, ClipText = true };
    private readonly Label _staticNote = new() { Text = Loc.GetString("wf-observer-preset-prototype-static-note"), StyleClasses = { "LabelSubText" } };
    private readonly BoxContainer _characterRow = Row();
    private readonly OptionButton _character = new() { HorizontalExpand = true };
    private readonly Label _noCharacters = new() { Text = Loc.GetString("wf-observer-preset-no-characters"), StyleClasses = { "LabelSubText" } };
    private readonly BoxContainer _outfitRow = Row();
    private readonly Label _outfit = new() { HorizontalExpand = true, ClipText = true };
    private readonly BoxContainer _dressRow = Row();
    private readonly OptionButton _entry = new();

    private readonly OptionButton _wording = new();
    private readonly Dictionary<WFMarkerVisuals, CheckBox> _visualBoxes = new();
    private readonly Dictionary<WFMarkerVisuals, BoxContainer> _lookRows = new();
    private readonly Label _needOne = new() { Text = Loc.GetString("wf-staff-marker-need-one-visual"), Visible = false, StyleClasses = { "LabelSubText" } };
    private readonly LineEdit _colour = new() { MinSize = new Vector2(110, 0) };
    private readonly Label _colourFixed = new() { Text = Loc.GetString("wf-observer-panel-marker-colour-fixed"), StyleClasses = { "LabelSubText" } };

    private string? _selected;
    private bool _wasManifested;
    private string _drawn = string.Empty;

    /// <summary>The kind a preset is being switched to while its prototype is still to be picked.</summary>
    private WFAvatarKind? _pendingKind;

    public WFObserverPresenceTab()
    {
        IoCManager.InjectDependencies(this);
        _data = _entities.System<WFAdminObserverClientSystem>();
        _manifest = _entities.System<WFManifestClientSystem>();
        _panel = _entities.System<WFObserverPanelClientSystem>();
        _sprite = _entities.System<SpriteSystem>();

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        Margin = new Thickness(8);
        HorizontalExpand = true;

        BuildPresence();
        BuildEditor();
        BuildMarker();
        Refresh();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _data.DataChanged += Refresh;
        _data.Request();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _data.DataChanged -= Refresh;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // Manifesting is not part of the saved document, so the state line watches for it on its own.
        var drawn = _manifest.LocalManifested() && _player.LocalEntity is { } local
            ? _entities.GetComponent<MetaDataComponent>(local).EntityName
            : string.Empty;
        if (drawn != _drawn || _manifest.LocalManifested() != _wasManifested)
            RefreshState();
    }

    private static BoxContainer Column() => new() { Orientation = LayoutOrientation.Vertical, SeparationOverride = 4 };

    private static BoxContainer Row() => new() { Orientation = LayoutOrientation.Horizontal, SeparationOverride = 6 };

    private static Label Heading(string key) => new() { Text = Loc.GetString(key), StyleClasses = { "LabelHeading" } };

    private void BuildPresence()
    {
        var top = Row();
        top.AddChild(_state);
        top.AddChild(_manifestButton);
        AddChild(top);
        _manifestButton.OnPressed += _ =>
        {
            if (_manifest.LocalManifested())
                _console.ExecuteCommand("unmanifest");
            else
                _console.ExecuteCommand(_selected == null ? "manifest" : $"manifest \"{_selected}\"");
        };

        AddChild(Heading("wf-observer-panel-section-presets"));
        var list = Row();
        list.AddChild(_presets);
        var buttons = Column();
        var add = new Button { Text = Loc.GetString("wf-observer-preset-new") };
        add.OnPressed += _ => _panel.SavePreset(null, new WFAvatarPreset());
        buttons.AddChild(add);
        buttons.AddChild(_edit);
        buttons.AddChild(_makeDefault);
        buttons.AddChild(_delete);
        list.AddChild(buttons);
        AddChild(list);

        _presets.OnItemSelected += args =>
        {
            _selected = _presets[args.ItemIndex].Metadata as string;
            RefreshEditor();
        };
        _edit.OnToggled += _ => RefreshEditor();
        _makeDefault.OnPressed += _ =>
        {
            if (_selected != null)
                _panel.SetDefault(_data.Data?.DefaultPreset == _selected ? null : _selected);
        };
        _delete.OnPressed += _ =>
        {
            if (_selected == null)
                return;

            _panel.DeletePreset(_selected);
            _selected = null;
        };
    }

    private void BuildEditor()
    {
        foreach (var kind in Kinds)
        {
            _kind.AddItem(Loc.GetString($"wf-observer-kind-{kind.ToString().ToLowerInvariant()}"), (int) kind);
        }

        _kind.OnItemSelected += args =>
        {
            var kind = (WFAvatarKind) args.Id;
            if (kind is WFAvatarKind.Object or WFAvatarKind.Mob && Selected()?.Prototype == null)
            {
                // An object or mob is nothing without its prototype: pick first, and save both together.
                _pendingKind = kind;
                PickPrototype();
                return;
            }

            Change(p => p.Kind = kind);
        };
        _editor.AddChild(_kind);

        _name.OnTextEntered += _ => Change(p => p.DisplayName = _name.Text.Trim());
        _name.OnFocusExit += _ => Change(p => p.DisplayName = _name.Text.Trim());
        _editor.AddChild(_name);
        _description.OnTextEntered += _ => Change(p => p.Description = _description.Text.Trim());
        _description.OnFocusExit += _ => Change(p => p.Description = _description.Text.Trim());
        _editor.AddChild(_description);

        var pick = new Button { Text = Loc.GetString("wf-observer-preset-pick-prototype") };
        pick.OnPressed += _ => PickPrototype();
        _prototypeRow.AddChild(pick);
        _prototypeRow.AddChild(_prototype);
        _editor.AddChild(_prototypeRow);
        _editor.AddChild(_staticNote);

        _characterRow.AddChild(new Label { Text = Loc.GetString("wf-observer-preset-pick-character") });
        _characterRow.AddChild(_character);
        _character.OnItemSelected += args => Change(p => p.CharacterSlot = args.Id);
        _editor.AddChild(_characterRow);
        _editor.AddChild(_noCharacters);

        var pickOutfit = new Button { Text = Loc.GetString("wf-observer-preset-pick-outfit") };
        pickOutfit.OnPressed += _ =>
        {
            var menu = new SpawnOutfitMenu(OutfitMenuMode.Pick)
            {
                Title = Loc.GetString("wf-observer-preset-pick-outfit"),
                ConfirmText = Loc.GetString("wf-observer-preset-pick-outfit-confirm"),
            };

            // A chosen outfit replaces one dressed by hand.
            menu.OnPicked += gear => Change(p =>
            {
                p.Outfit = gear;
                p.OutfitLayout = null;
            });
            menu.OpenCentered();
        };
        _outfitRow.AddChild(pickOutfit);
        _outfitRow.AddChild(_outfit);
        _editor.AddChild(_outfitRow);

        var dress = new Button { Text = Loc.GetString("wf-observer-preset-dress-avatar") };
        dress.OnPressed += _ =>
        {
            if (_selected != null)
                _panel.Dress(_selected);
        };
        var save = new Button { Text = Loc.GetString("wf-observer-preset-save-outfit") };
        save.OnPressed += _ =>
        {
            if (_selected != null)
                _panel.SaveOutfit(_selected);
        };
        _dressRow.AddChild(dress);
        _dressRow.AddChild(save);
        _editor.AddChild(_dressRow);

        var arrival = Row();
        arrival.AddChild(new Label { Text = Loc.GetString("wf-observer-panel-section-entry") });
        _entry.AddItem(Loc.GetString("wf-observer-entry-bluespace"), (int) WFEntryStyle.Bluespace);
        _entry.AddItem(Loc.GetString("wf-observer-entry-silent"), (int) WFEntryStyle.Silent);
        _entry.OnItemSelected += args => Change(p => p.EntryStyle = (WFEntryStyle) args.Id);
        arrival.AddChild(_entry);
        _editor.AddChild(arrival);

        AddChild(_editor);
    }

    private void BuildMarker()
    {
        AddChild(Heading("wf-observer-panel-section-marker"));

        var wording = Row();
        wording.AddChild(new Label { Text = Loc.GetString("wf-observer-panel-marker-wording") });
        foreach (var option in Wordings)
        {
            _wording.AddItem(Loc.GetString(WordingKey(option)), (int) option);
        }

        _wording.OnItemSelected += args => ChangeMarker(m => m.Wording = (WFMarkerWording) args.Id);
        wording.AddChild(_wording);
        AddChild(wording);

        AddChild(new Label { Text = Loc.GetString("wf-observer-panel-marker-visuals") });
        foreach (var visual in Visuals)
        {
            var key = visual.ToString().ToLowerInvariant();
            var box = new CheckBox
            {
                Text = Loc.GetString($"wf-staff-marker-visual-{key}"),
                ToolTip = Loc.GetString($"wf-staff-marker-visual-{key}-desc"),
                MinSize = new Vector2(130, 0),
            };
            box.OnToggled += args => ToggleVisual(visual, args.Pressed);
            _visualBoxes[visual] = box;

            var row = Row();
            row.AddChild(box);
            var looks = Row();
            _lookRows[visual] = looks;
            row.AddChild(looks);
            AddChild(row);
        }

        AddChild(_needOne);

        var colour = Row();
        colour.AddChild(new Label { Text = Loc.GetString("wf-observer-panel-marker-colour") });
        colour.AddChild(_colour);
        colour.AddChild(_colourFixed);
        _colour.OnTextEntered += _ => SendColour();
        _colour.OnFocusExit += _ => SendColour();
        AddChild(colour);
    }

    private static string WordingKey(WFMarkerWording wording)
    {
        return wording switch
        {
            WFMarkerWording.Staff => "wf-staff-marker-wording-staff",
            WFMarkerWording.Name => "wf-staff-marker-wording-name",
            WFMarkerWording.RankAndName => "wf-staff-marker-wording-rank-and-name",
            _ => "wf-staff-marker-wording-rank",
        };
    }

    private WFAvatarPreset? Selected()
    {
        return _selected == null ? null : _data.Data?.Presets.FirstOrDefault(p => p.Name == _selected);
    }

    /// <summary>Sends the selected preset with one thing changed. The server's answer redraws the tab.</summary>
    private void Change(Action<WFAvatarPreset> change)
    {
        if (Selected() is not { } preset)
            return;

        var copy = new WFAvatarPreset
        {
            Name = preset.Name,
            Kind = preset.Kind,
            Prototype = preset.Prototype,
            CharacterSlot = preset.CharacterSlot,
            Outfit = preset.Outfit,
            OutfitLayout = preset.OutfitLayout,
            DisplayName = preset.DisplayName,
            Description = preset.Description,
            EntryStyle = preset.EntryStyle,
        };
        change(copy);

        // Nothing typed and nothing changed: a field losing focus is not an edit.
        if (copy.Kind == preset.Kind && copy.Prototype == preset.Prototype && copy.CharacterSlot == preset.CharacterSlot
            && copy.Outfit == preset.Outfit && ReferenceEquals(copy.OutfitLayout, preset.OutfitLayout)
            && copy.DisplayName == preset.DisplayName && copy.Description == preset.Description
            && copy.EntryStyle == preset.EntryStyle)
        {
            return;
        }

        // The server names a preset after what players will see, so follow the name it is about to take.
        var original = preset.Name;
        if (copy.DisplayName.Length > 0 && _data.Data!.Presets.All(p => p == preset || p.Name != copy.DisplayName))
            _selected = copy.DisplayName.Length > WFAdminObserverData.MaxNameLength ? copy.DisplayName[..WFAdminObserverData.MaxNameLength] : copy.DisplayName;

        _panel.SavePreset(original, copy);
    }

    private void PickPrototype()
    {
        var picker = new WFAvatarPickerWindow();
        picker.OnPicked += id =>
        {
            var kind = _pendingKind;
            _pendingKind = null;
            Change(p =>
            {
                p.Prototype = id;
                if (kind != null)
                    p.Kind = kind.Value;
            });
        };
        picker.OnClose += () =>
        {
            _pendingKind = null;
            RefreshEditor();
        };
        picker.OpenCentered();
    }

    private void ChangeMarker(Action<WFStaffMarkerSettings> change)
    {
        if (_data.Data is not { } data)
            return;

        var marker = data.Marker;
        var copy = new WFStaffMarkerSettings
        {
            Wording = marker.Wording,
            Visuals = marker.Visuals,
            Ring = marker.Ring,
            Glyph = marker.Glyph,
            Shield = marker.Shield,
            Aura = marker.Aura,
            Color = marker.Color,
        };
        change(copy);
        _panel.SetMarker(copy);
    }

    private void ToggleVisual(WFMarkerVisuals visual, bool on)
    {
        if (_data.Data is not { } data)
            return;

        var visuals = on ? data.Marker.Visuals | visual : data.Marker.Visuals & ~visual;
        if (visuals == WFMarkerVisuals.None)
        {
            // The last one stays on.
            _needOne.Visible = true;
            _visualBoxes[visual].Pressed = true;
            return;
        }

        _needOne.Visible = false;
        ChangeMarker(m => m.Visuals = visuals);
    }

    private void SendColour()
    {
        var text = _colour.Text.Trim();
        if (_data.Data is not { } data || text == (data.Marker.Color ?? string.Empty))
            return;

        if (text.Length == 0)
            ChangeMarker(m => m.Color = null);
        else if (Color.TryFromHex(text, out _))
            ChangeMarker(m => m.Color = text);
        else
            _colour.Text = data.Marker.Color ?? string.Empty;
    }

    private void Refresh()
    {
        RefreshState();
        RefreshPresets();
        RefreshEditor();
        RefreshMarker();
    }

    private void RefreshState()
    {
        _wasManifested = _manifest.LocalManifested();
        _drawn = _wasManifested && _player.LocalEntity is { } local
            ? _entities.GetComponent<MetaDataComponent>(local).EntityName
            : string.Empty;

        _state.Text = _wasManifested
            ? Loc.GetString("wf-observer-panel-state-manifested", ("name", _drawn))
            : Loc.GetString("wf-observer-panel-state-hidden");
        _manifestButton.Text = Loc.GetString(_wasManifested ? "wf-observer-panel-hide-button" : "wf-observer-panel-manifest-button");
    }

    private void RefreshPresets()
    {
        _presets.Clear();
        if (_data.Data is not { } data)
            return;

        if (_selected != null && data.Presets.All(p => p.Name != _selected))
            _selected = null;

        foreach (var preset in data.Presets)
        {
            var text = preset.Name == data.DefaultPreset
                ? $"{preset.Name} [{Loc.GetString("wf-observer-preset-default-tag")}]"
                : preset.Name;
            var item = _presets.AddItem(text);
            item.Metadata = preset.Name;
            if (preset.Name == _selected)
                item.Selected = true;
        }
    }

    private void RefreshEditor()
    {
        var preset = Selected();
        _edit.Disabled = _makeDefault.Disabled = _delete.Disabled = preset == null;
        _editor.Visible = preset != null && _edit.Pressed;
        if (preset == null)
            return;

        _kind.SelectId((int) (_pendingKind ?? preset.Kind));
        _name.Text = preset.DisplayName;
        _description.Text = preset.Description;
        _entry.SelectId((int) preset.EntryStyle);

        var thing = preset.Kind is WFAvatarKind.Object or WFAvatarKind.Mob;
        _prototypeRow.Visible = _staticNote.Visible = thing;
        _prototype.Text = thing && _prototypes.TryIndex<EntityPrototype>(preset.Prototype ?? string.Empty, out var prototype)
            ? prototype.Name
            : preset.Prototype ?? string.Empty;

        var character = preset.Kind == WFAvatarKind.Character;
        _outfitRow.Visible = _dressRow.Visible = character;
        _outfit.Text = preset.Outfit ?? string.Empty;

        _character.Clear();
        var characters = _preferences.Preferences?.Characters;
        if (characters != null)
        {
            foreach (var (slot, profile) in characters.OrderBy(c => c.Key))
            {
                if (profile is HumanoidCharacterProfile)
                    _character.AddItem(profile.Name, slot);
            }
        }

        _characterRow.Visible = character && _character.ItemCount > 0;
        _noCharacters.Visible = character && _character.ItemCount == 0;
        var chosen = preset.CharacterSlot ?? _preferences.Preferences?.SelectedCharacterIndex ?? 0;
        if (_character.ItemCount > 0)
            _character.TrySelectId(chosen);
    }

    private void RefreshMarker()
    {
        if (_data.Data is not { } data)
            return;

        var marker = data.Marker;
        _wording.SelectId((int) marker.Wording);
        foreach (var (visual, box) in _visualBoxes)
        {
            box.Pressed = (marker.Visuals & visual) != 0;
            RefreshLooks(visual, marker);
        }

        var host = _admin.HasFlag(AdminFlags.Host);
        _colour.Visible = host;
        _colourFixed.Visible = !host;
        if (!_colour.HasKeyboardFocus())
            _colour.Text = marker.Color ?? string.Empty;
    }

    /// <summary>The looks of one visual: a named choice for the shield and aura, pictures for the ring and glyph.</summary>
    private void RefreshLooks(WFMarkerVisuals visual, WFStaffMarkerSettings marker)
    {
        var row = _lookRows[visual];
        row.RemoveAllChildren();

        var looks = _prototypes.EnumeratePrototypes<WFStaffMarkerVariantPrototype>()
            .Where(v => v.Visual == visual)
            .OrderBy(v => v.Order)
            .ToList();
        if (looks.Count < 2)
            return;

        var chosen = visual switch
        {
            WFMarkerVisuals.Ring => marker.Ring,
            WFMarkerVisuals.Glyph => marker.Glyph,
            WFMarkerVisuals.Shield => marker.Shield,
            _ => marker.Aura,
        };
        var current = looks.FirstOrDefault(v => v.ID == chosen) ?? looks.FirstOrDefault(v => v.Default) ?? looks[0];

        void Choose(string id)
        {
            ChangeMarker(m =>
            {
                switch (visual)
                {
                    case WFMarkerVisuals.Ring:
                        m.Ring = id;
                        break;
                    case WFMarkerVisuals.Glyph:
                        m.Glyph = id;
                        break;
                    case WFMarkerVisuals.Shield:
                        m.Shield = id;
                        break;
                    default:
                        m.Aura = id;
                        break;
                }
            });
        }

        row.AddChild(new Label { Text = Loc.GetString("wf-observer-panel-marker-style") });

        if (looks.All(v => v.Sprite != null))
        {
            var group = new ButtonGroup();
            foreach (var look in looks)
            {
                var button = new Button { ToggleMode = true, Group = group, Pressed = look == current, MinSize = new Vector2(40, 40) };
                button.AddChild(new TextureRect
                {
                    Texture = _sprite.Frame0(look.Sprite!),
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    MinSize = new Vector2(32, 32),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                });
                var id = look.ID;
                button.OnPressed += _ => Choose(id);
                row.AddChild(button);
            }

            return;
        }

        var options = new OptionButton();
        for (var i = 0; i < looks.Count; i++)
        {
            options.AddItem(looks[i].Name is { } name ? Loc.GetString(name) : looks[i].ID, i);
        }

        options.SelectId(looks.IndexOf(current));
        options.OnItemSelected += args => Choose(looks[args.Id].ID);
        row.AddChild(options);
    }
}
