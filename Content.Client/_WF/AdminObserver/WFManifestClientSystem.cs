using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._WF.AdminObserver;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client._WF.AdminObserver;

/// <summary>
/// The client side of manifesting: the local admin's chat channels follow whether their observer is manifested,
/// and every staff marker in view gets its nameplate.
/// </summary>
public sealed partial class WFManifestClientSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private WFAvatarVisualsSystem _avatar = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WFManifestedComponent, ComponentStartup>((uid, _, _) => RefreshChat(uid));
        SubscribeLocalEvent<WFManifestedComponent, ComponentRemove>(OnRemove);
        _overlays.AddOverlay(new WFStaffNameplateOverlay(EntityManager, _eye, _lookup, _resources, _ui));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay<WFStaffNameplateOverlay>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // A client that has hidden ghosts, as one does on leaving a ghost for a body, must still see a manifested one.
        var query = EntityQueryEnumerator<WFManifestedComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (!sprite.Visible)
                _sprite.SetVisible((uid, sprite), true);
        }
    }

    /// <summary>Whether the local player is a manifested admin observer.</summary>
    public bool LocalManifested()
    {
        return _player.LocalEntity is { } local && HasComp<WFManifestedComponent>(local);
    }

    private void OnRemove(Entity<WFManifestedComponent> ent, ref ComponentRemove args)
    {
        RefreshChat(ent);
        _avatar.Restore(ent);

        // Hidden again, the observer is a ghost like any other to this client.
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), _ghost.WFGhostsVisible || ent.Owner == _player.LocalEntity);
    }

    private void RefreshChat(EntityUid uid)
    {
        if (uid == _player.LocalEntity)
            _ui.GetUIController<ChatUIController>().WFRefreshChannelPermissions();
    }
}
