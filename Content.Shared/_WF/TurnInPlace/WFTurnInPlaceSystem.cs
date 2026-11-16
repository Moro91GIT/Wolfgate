using Content.Shared.Movement.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared._WF.TurnInPlace;

/// <summary>Tracks the held turn-in-place key; <see cref="SharedMoverController"/> reads it on movement input.</summary>
public sealed class WFTurnInPlaceSystem : EntitySystem
{
    [Dependency] private SharedMoverController _mover = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(WFTurnInPlaceKeys.WFTurnInPlace, new TurnInPlaceInputCmdHandler(this))
            .Register<WFTurnInPlaceSystem>();

        // The key's release goes to whatever the player controls by then, so a body left behind keeps no hold.
        SubscribeLocalEvent<WFTurnInPlaceComponent, PlayerDetachedEvent>((uid, _, _) => RemComp<WFTurnInPlaceComponent>(uid));
        SubscribeLocalEvent<WFTurnInPlaceComponent, LocalPlayerDetachedEvent>((uid, _, _) => RemComp<WFTurnInPlaceComponent>(uid));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<WFTurnInPlaceSystem>();
    }

    /// <summary>Holds or releases the turn key for an entity. Holding it stops the mob where it stands.</summary>
    public void SetHeld(EntityUid uid, bool held, ushort subTick = 0)
    {
        if (!held)
        {
            RemComp<WFTurnInPlaceComponent>(uid);
            return;
        }

        if (HasComp<WFTurnInPlaceComponent>(uid))
            return;

        AddComp<WFTurnInPlaceComponent>(uid);
        _mover.WfReleaseDirections(uid, subTick);
    }

    private sealed class TurnInPlaceInputCmdHandler(WFTurnInPlaceSystem system) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            if (session?.AttachedEntity is not { } uid)
                return false;

            system.SetHeld(uid, message.State == BoundKeyState.Down, message.SubTick);
            return false;
        }
    }
}
