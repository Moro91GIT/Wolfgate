using Content.Client.UserInterface.Fragments;
using Content.Shared._WF.JudgeLine;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._WF.JudgeLine;

/// <summary>The Judge Line PDA app.</summary>
public sealed partial class JudgeLineUi : UIFragment
{
    private WFJudgeLinePanel? _panel;

    public override Control GetUIFragmentRoot()
    {
        return _panel!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _panel = new WFJudgeLinePanel();
        _panel.OnCall += kind => userInterface.SendMessage(new CartridgeUiMessage(new WFJudgeLineCallMessage(kind)));
        _panel.OnCancel += () => userInterface.SendMessage(new CartridgeUiMessage(new WFJudgeLineCancelMessage()));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is WFJudgeLineUiState cast)
            _panel?.UpdateState(cast);
    }
}

/// <summary>
/// The app's page: a button per kind of emergency with the fee, or, once a call is open, where it stands and a cancel
/// button. A refused caller sees why.
/// </summary>
public sealed class WFJudgeLinePanel : BoxContainer
{
    public event Action<WFJudgeLineKind>? OnCall;
    public event Action? OnCancel;

    private readonly Label _header = new() { StyleClasses = { "LabelHeading" } };
    private readonly RichTextLabel _info = new();
    private readonly BoxContainer _buttons = new() { Orientation = LayoutOrientation.Vertical, SeparationOverride = 4 };
    private readonly Button _cancel = new() { Text = Loc.GetString("wf-judgeline-app-cancel"), HorizontalAlignment = HAlignment.Left };

    public WFJudgeLinePanel()
    {
        Orientation = LayoutOrientation.Vertical;
        Margin = new Thickness(8);
        SeparationOverride = 8;
        _header.Text = Loc.GetString("wf-judgeline-app-title");
        AddChild(_header);
        AddChild(_info);
        AddChild(_buttons);
        AddChild(_cancel);
        _cancel.OnPressed += _ => OnCancel?.Invoke();

        foreach (var kind in Enum.GetValues<WFJudgeLineKind>())
        {
            var name = kind.ToString().ToLowerInvariant();
            var row = new BoxContainer { Orientation = LayoutOrientation.Vertical };
            var button = new Button { Text = Loc.GetString($"wf-judgeline-kind-{name}"), HorizontalExpand = true };
            button.OnPressed += _ => OnCall?.Invoke(kind);
            row.AddChild(button);
            row.AddChild(new Label
            {
                Text = Loc.GetString($"wf-judgeline-kind-{name}-hint"),
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(4, 0, 0, 0),
            });
            _buttons.AddChild(row);
        }
    }

    public void UpdateState(WFJudgeLineUiState state)
    {
        var open = state.Phase != WFJudgeLinePhase.None;
        _buttons.Visible = !open;
        _cancel.Visible = open && state.Phase is WFJudgeLinePhase.Queued or WFJudgeLinePhase.Dispatched;
        foreach (var row in _buttons.Children)
        {
            if (row.ChildCount > 0 && row.GetChild(0) is Button button)
                button.Disabled = state.Refusal != null;
        }

        if (!open)
        {
            var intro = Loc.GetString("wf-judgeline-app-intro", ("fee", state.Fee));
            _info.SetMessage(state.Refusal == null ? intro : $"{intro}\n\n[color=#d05050]{state.Refusal}[/color]");
            return;
        }

        var phase = state.Phase switch
        {
            WFJudgeLinePhase.Queued => Loc.GetString("wf-judgeline-app-queued", ("place", state.Queue)),
            WFJudgeLinePhase.Dispatched => Loc.GetString("wf-judgeline-app-dispatched", ("seconds", state.Eta)),
            _ => Loc.GetString("wf-judgeline-app-on-scene"),
        };
        var kind = Loc.GetString($"wf-judgeline-kind-{state.Kind.ToString().ToLowerInvariant()}");
        _info.SetMessage(Loc.GetString("wf-judgeline-app-open", ("kind", kind), ("phase", phase),
            ("status", state.Status ?? string.Empty)));
    }
}
