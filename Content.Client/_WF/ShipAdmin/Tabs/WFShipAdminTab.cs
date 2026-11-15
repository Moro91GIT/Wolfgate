using Content.Shared._WF.ShipAdmin;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._WF.ShipAdmin.Tabs;

/// <summary>One tab of the admin ship panel: shows part of the selected ship and raises the messages its buttons send.</summary>
public abstract class WFShipAdminTab : ScrollContainer
{
    /// <summary>Raised with a message for the server.</summary>
    public event Action<EuiMessageBase>? OnMessage;

    /// <summary>Where the tab puts its controls.</summary>
    protected readonly BoxContainer Body;

    /// <summary>The ship the tab last showed.</summary>
    protected NetEntity Grid;

    protected WFShipAdminTab()
    {
        HScrollEnabled = false;
        VerticalExpand = true;
        Body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };
        AddChild(Body);
    }

    /// <summary>The tab's title in the panel.</summary>
    public abstract string Title { get; }

    /// <summary>Shows a ship, greying what the viewer's flags don't allow.</summary>
    public void Update(WFShipAdminDetail detail, AdminFlags flags)
    {
        Grid = detail.Grid;
        Show(detail, flags);
    }

    protected abstract void Show(WFShipAdminDetail detail, AdminFlags flags);

    protected void Send(EuiMessageBase message)
    {
        OnMessage?.Invoke(message);
    }

    /// <summary>Disables a button the viewer's flags don't allow and says which permission it needs.</summary>
    protected static void Gate(BaseButton button, WFShipAdminAction action, AdminFlags flags, bool enabled = true)
    {
        var allowed = WFShipAdminPermissions.Allowed(flags, action);
        button.Disabled = !allowed || !enabled;
        if (!allowed)
            button.ToolTip = Loc.GetString("wf-ship-admin-needs-flag", ("flag", WFShipAdminPermissions.Required(action).ToString()));
    }

    protected static Label Heading(string text)
    {
        return new Label { Text = text, StyleClasses = { "LabelHeading" }, Margin = new Thickness(0, 6, 0, 0) };
    }

    /// <summary>A "key: value" line.</summary>
    protected static Control Line(string key, string value)
    {
        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        row.AddChild(new Label { Text = key, StyleClasses = { "LabelKeyText" }, MinWidth = 130 });
        row.AddChild(new Label { Text = value, HorizontalExpand = true, ClipText = true });
        return row;
    }

    protected static Label Note(string text)
    {
        return new Label { Text = text, StyleClasses = { "LabelSubText" } };
    }
}
