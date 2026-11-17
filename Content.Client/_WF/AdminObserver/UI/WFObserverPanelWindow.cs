using System.Numerics;
using Content.Client.Ghost;
using Robust.Client.UserInterface.CustomControls;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared._WF.Administration;
using Content.Shared._WF.AdminObserver;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._WF.AdminObserver.UI;

/// <summary>
/// The observer panel: who the admin appears as and how they are marked (Presence), what they carry (Kit), the
/// consoles the observer has built in (Tools) and the windows for putting people in the round (People).
/// </summary>
public sealed class WFObserverPanelWindow : DefaultWindow
{
    [Dependency] private IClientConsoleHost _console = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    /// <summary>The built-in consoles that are listed under their own action's name.</summary>
    private static readonly (WFObserverTool Tool, EntProtoId Action)[] Consoles =
    {
        (WFObserverTool.Solar, "ActionAGhostShowSolar"),
        (WFObserverTool.Communications, "ActionAGhostShowCommunications"),
        (WFObserverTool.Cargo, "ActionAGhostShowCargo"),
        (WFObserverTool.CrewMonitor, "ActionAGhostShowCrewMonitoring"),
        (WFObserverTool.Records, "ActionAGhostShowStationRecords"),
    };

    public WFObserverPanelWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("wf-observer-panel-title");
        MinSize = new Vector2(520, 560);
        SetSize = new Vector2(580, 720);

        var tabs = new TabContainer { VerticalExpand = true, Margin = new Thickness(4) };
        AddTab(tabs, Scrolling(new WFObserverPresenceTab()), "wf-observer-panel-tab-presence");
        AddTab(tabs, new WFAdminKitControl { Margin = new Thickness(4) }, "wf-observer-panel-tab-kit");
        AddTab(tabs, Tools(), "wf-observer-panel-tab-tools");
        AddTab(tabs, People(), "wf-observer-panel-tab-people");
        Contents.AddChild(tabs);
    }

    private static void AddTab(TabContainer tabs, Control tab, string title)
    {
        tabs.AddChild(tab);
        TabContainer.SetTabTitle(tab, Loc.GetString(title));
    }

    private static Control Scrolling(Control content)
    {
        var scroll = new ScrollContainer { HScrollEnabled = false, VerticalExpand = true, HorizontalExpand = true };
        scroll.AddChild(content);
        return scroll;
    }

    private static BoxContainer Column()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(8),
        };
    }

    private Control Tools()
    {
        var panel = _entities.System<WFObserverPanelClientSystem>();
        var column = Column();

        var scanner = new Button { Text = Loc.GetString("wf-observer-panel-tool-radar") };
        scanner.OnPressed += _ => panel.OpenTool(WFObserverTool.Radar);
        column.AddChild(scanner);

        var orbit = new Button { Text = Loc.GetString("wf-observer-panel-tool-orbit") };
        orbit.OnPressed += _ => OpenOrbitMenu();
        column.AddChild(orbit);

        foreach (var (tool, action) in Consoles)
        {
            if (!_prototypes.TryIndex(action, out var prototype))
                continue;

            var button = new Button { Text = prototype.Name };
            button.OnPressed += _ => panel.OpenTool(tool);
            column.AddChild(button);
        }

        return column;
    }

    /// <summary>The ghost orbit menu, as its own button on the ghost bar opens it.</summary>
    private void OpenOrbitMenu()
    {
        if (_ui.GetActiveUIWidgetOrNull<GhostGui>() is not { } gui)
            return;

        _entities.System<GhostSystem>().RequestWarps();
        gui.TargetWindow.Populate();
        gui.TargetWindow.OpenCentered();
    }

    private Control People()
    {
        var column = Column();

        // Spawn as Outfit works at an entity; from here that is wherever the admin is.
        var outfit = new Button { Text = Loc.GetString("wf-admin-verbs-spawn-outfit") };
        outfit.OnPressed += _ =>
        {
            if (_player.LocalEntity is { } local)
                _console.ExecuteCommand($"{WolfgateAdminCommands.SpawnOutfitUi} {_entities.GetNetEntity(local).Id}");
        };
        column.AddChild(outfit);

        var ert = new Button { Text = Loc.GetString("wf-admin-tab-ert") };
        ert.OnPressed += _ => _console.ExecuteCommand(WolfgateAdminCommands.ErtBuilderUi);
        column.AddChild(ert);

        return column;
    }
}
