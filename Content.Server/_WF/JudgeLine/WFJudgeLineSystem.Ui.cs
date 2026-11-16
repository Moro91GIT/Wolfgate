using Content.Server.Shuttles.Components;
using Content.Shared._WF.JudgeLine;
using Content.Shared.CartridgeLoader;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._WF.JudgeLine;

/// <summary>The Judge Line app and the shuttle console's call verbs.</summary>
public sealed partial class WFJudgeLineSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private static readonly VerbCategory VerbCategory = new("wf-judgeline-verb-category", null);

    private void InitializeUi()
    {
        SubscribeLocalEvent<WFJudgeLineCartridgeComponent, CartridgeUiReadyEvent>(OnCartridgeReady);
        SubscribeLocalEvent<WFJudgeLineCartridgeComponent, CartridgeMessageEvent>(OnCartridgeMessage);
        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnCartridgeReady(Entity<WFJudgeLineCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        PushCartridge(args.Loader);
    }

    private void OnCartridgeMessage(Entity<WFJudgeLineCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        var loader = GetEntity(args.LoaderUid);
        // Only the PDA's carrier may use it.
        if (Transform(loader).ParentUid != args.Actor)
            return;

        switch (args)
        {
            case WFJudgeLineCallMessage call:
                Call(args.Actor, call.Kind);
                break;
            case WFJudgeLineCancelMessage:
                Cancel(args.Actor);
                break;
        }

        PushCartridge(loader);
    }

    /// <summary>Places a call for someone, telling them why not if it is refused.</summary>
    private void Call(EntityUid user, WFJudgeLineKind kind)
    {
        if (!TryCall(user, kind, out var refusal) && refusal != null)
            _popup.PopupEntity(refusal, user, user, PopupType.MediumCaution);
    }

    /// <summary>A vagrant at a shuttle console can call the judges for each kind of emergency, or cancel their call.</summary>
    private void OnGetVerbs(GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<ShuttleConsoleComponent>(args.Target)
            || !_config.GetCVar(JudgeLineCVars.Enabled) || !IsVagrant(args.User))
            return;

        var user = args.User;
        if (CallOf(user) != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("wf-judgeline-verb-cancel"),
                Category = VerbCategory,
                Act = () => Cancel(user),
                Priority = -10,
            });
            return;
        }

        var fee = _config.GetCVar(JudgeLineCVars.Fee);
        foreach (var kind in Enum.GetValues<WFJudgeLineKind>())
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = KindName(kind),
                Category = VerbCategory,
                Message = Loc.GetString("wf-judgeline-verb-confirm", ("fee", fee)),
                ConfirmationPopup = true,
                Act = () => Call(user, kind),
                Priority = -10 - (int) kind,
            });
        }
    }

    /// <summary>What the Judge Line app shows someone.</summary>
    public WFJudgeLineUiState State(EntityUid viewer)
    {
        var state = new WFJudgeLineUiState { Fee = _config.GetCVar(JudgeLineCVars.Fee) };
        if (CallOf(viewer) is { } call)
        {
            state.Phase = call.Phase;
            state.Kind = call.Kind;
            state.Status = call.Status;
            state.Eta = call.Phase == WFJudgeLinePhase.Dispatched
                ? Math.Max(0, (int) Math.Ceiling((call.DueAt - _timing.CurTime).TotalSeconds))
                : 0;
            var place = 1;
            foreach (var other in _calls)
            {
                if (other == call)
                    break;
                if (other.Phase == WFJudgeLinePhase.Queued)
                    place++;
            }

            state.Queue = call.Phase == WFJudgeLinePhase.Queued ? place : 0;
            return state;
        }

        state.Refusal = Refusal(viewer);
        return state;
    }

    private void PushCartridge(EntityUid loader)
    {
        var holder = Transform(loader).ParentUid;
        if (!TryComp<ActorComponent>(holder, out var actor))
            return;

        _cartridgeLoader.UpdateCartridgeUiState(loader, State(holder), actor.PlayerSession);
    }

    /// <summary>Keeps every open app current.</summary>
    private void RefreshOpen()
    {
        var cartridges = EntityQueryEnumerator<WFJudgeLineCartridgeComponent, CartridgeComponent>();
        while (cartridges.MoveNext(out var uid, out _, out var cartridge))
        {
            if (cartridge.LoaderUid is { } loader
                && TryComp<CartridgeLoaderComponent>(loader, out var loaderComp)
                && loaderComp.ActiveProgram == uid
                && _ui.IsUiOpen(loader, loaderComp.UiKey))
                PushCartridge(loader);
        }
    }
}
