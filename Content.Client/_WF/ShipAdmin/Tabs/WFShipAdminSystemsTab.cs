using Content.Client.UserInterface.Controls;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared._WF.ShipAdmin;
using Content.Shared.Administration;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._WF.ShipAdmin.Tabs;

/// <summary>A ship's name, radar signature, shields, power and the protections staff can switch on it.</summary>
public sealed class WFShipAdminSystemsTab : WFShipAdminTab
{
    private readonly LineEdit _name;
    private readonly Button _rename;
    private readonly LineEdit _colour;
    private readonly Button _setColour;
    private readonly CheckBox _hidden;
    private readonly CheckBox _labelHidden;
    private readonly Button _shieldsOn;
    private readonly Button _shieldsOff;
    private readonly Button _powerOn;
    private readonly ConfirmButton _powerOff;
    private readonly Dictionary<WFShipAdminProtect, CheckBox> _protect = new();

    private NetEntity _shown;

    public override string Title => Loc.GetString("wf-ship-admin-tab-systems");

    public WFShipAdminSystemsTab()
    {
        Body.AddChild(Heading(Loc.GetString("wf-ship-admin-systems-name")));
        _name = new LineEdit { MinWidth = 260 };
        _name.IsValid = text => text.Trim().Length > 0 && text.Trim().Length <= ShuttleDeedComponent.MaxNameLength;
        _rename = new Button { Text = Loc.GetString("wf-ship-admin-systems-rename"), StyleClasses = { "ButtonSquare" } };
        _rename.OnPressed += _ => Send(new WFShipAdminRenameMessage(Grid, _name.Text));
        Body.AddChild(Row(_name, _rename));

        Body.AddChild(Heading(Loc.GetString("wf-ship-admin-systems-radar")));
        _colour = new LineEdit { MinWidth = 110, PlaceHolder = Loc.GetString("wf-ship-admin-systems-colour-hint") };
        _setColour = new Button { Text = Loc.GetString("wf-ship-admin-systems-set-colour"), StyleClasses = { "ButtonSquare" } };
        _setColour.OnPressed += _ => Send(new WFShipAdminIffMessage(Grid, _colour.Text.Trim(), null, null));
        Body.AddChild(Row(_colour, _setColour));

        _hidden = new CheckBox { Text = Loc.GetString("wf-ship-admin-systems-hidden"), ToolTip = Loc.GetString("wf-ship-admin-systems-hidden-tooltip") };
        _hidden.OnToggled += args => Send(new WFShipAdminIffMessage(Grid, null, args.Pressed, null));
        _labelHidden = new CheckBox { Text = Loc.GetString("wf-ship-admin-systems-label-hidden"), ToolTip = Loc.GetString("wf-ship-admin-systems-label-hidden-tooltip") };
        _labelHidden.OnToggled += args => Send(new WFShipAdminIffMessage(Grid, null, null, args.Pressed));
        Body.AddChild(Row(_hidden, _labelHidden));

        Body.AddChild(Heading(Loc.GetString("wf-ship-admin-systems-shields-power")));
        _shieldsOn = Switch("wf-ship-admin-systems-shields-on", WFShipAdminAction.Shields, true);
        _shieldsOff = Switch("wf-ship-admin-systems-shields-off", WFShipAdminAction.Shields, false);
        _powerOn = Switch("wf-ship-admin-systems-power-on", WFShipAdminAction.Power, true);
        _powerOff = new ConfirmButton
        {
            Text = Loc.GetString("wf-ship-admin-systems-power-off"),
            ToolTip = Loc.GetString("wf-ship-admin-systems-power-off-tooltip"),
            ConfirmationText = Loc.GetString("wf-ship-admin-confirm"),
            StyleClasses = { "ButtonSquare" },
        };
        _powerOff.OnPressed += _ => Send(new WFShipAdminActionMessage(Grid, WFShipAdminAction.Power, false));
        Body.AddChild(Row(_shieldsOn, _shieldsOff, _powerOn, _powerOff));

        Body.AddChild(Heading(Loc.GetString("wf-ship-admin-systems-protect")));
        foreach (var protect in Enum.GetValues<WFShipAdminProtect>())
        {
            if (protect == WFShipAdminProtect.None)
                continue;

            var key = $"wf-ship-admin-systems-protect-{protect.ToString().ToLowerInvariant()}";
            var box = new CheckBox { Text = Loc.GetString(key), ToolTip = Loc.GetString($"{key}-tooltip") };
            var flag = protect;
            box.OnToggled += args => Send(new WFShipAdminProtectMessage(Grid, flag, args.Pressed));
            _protect[protect] = box;
            Body.AddChild(box);
        }
    }

    protected override void Show(WFShipAdminDetail detail, AdminFlags flags)
    {
        // The name and colour boxes are only refilled for another ship, so typing isn't wiped by a refresh.
        if (_shown != detail.Grid)
        {
            _shown = detail.Grid;
            _name.Text = detail.Name;
            _colour.Text = detail.IffColor;
        }

        _hidden.Pressed = detail.IffHidden;
        _labelHidden.Pressed = detail.IffLabelHidden;
        foreach (var (protect, box) in _protect)
        {
            box.Pressed = (detail.Protect & protect) != 0;
            Gate(box, WFShipAdminAction.Protect, flags);
        }

        Gate(_rename, WFShipAdminAction.Rename, flags);
        Gate(_setColour, WFShipAdminAction.Iff, flags);
        Gate(_hidden, WFShipAdminAction.Iff, flags);
        Gate(_labelHidden, WFShipAdminAction.Iff, flags);
        Gate(_shieldsOn, WFShipAdminAction.Shields, flags);
        Gate(_shieldsOff, WFShipAdminAction.Shields, flags);
        Gate(_powerOn, WFShipAdminAction.Power, flags);
        Gate(_powerOff, WFShipAdminAction.Power, flags);
    }

    private Button Switch(string key, WFShipAdminAction action, bool value)
    {
        var button = new Button { Text = Loc.GetString(key), StyleClasses = { "ButtonSquare" } };
        button.OnPressed += _ => Send(new WFShipAdminActionMessage(Grid, action, value));
        return button;
    }
}
