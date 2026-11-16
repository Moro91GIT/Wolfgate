#nullable enable
using System.Linq;
using Content.Client._WF.AdminObserver;
using Content.Server._WF.AdminObserver;
using Content.Shared._WF.AdminObserver;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WF.AdminObserver;

/// <summary>Object and mob avatars: which prototypes may be one, what they are called and how they are drawn.</summary>
[TestFixture]
[TestOf(typeof(WFManifestSystem))]
public sealed class AvatarPrototypeRulesTest
{
    private const string Avatar = "Crowbar";
    private const string NoSprite = "WFAvatarTestNoSprite";
    private const string Abstract = "WFAvatarTestAbstract";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {NoSprite}
  name: nothing to see

- type: entity
  id: {Abstract}
  abstract: true
  components:
  - type: Sprite
    sprite: Objects/Tools/crowbar.rsi
    state: icon
";

    [Test]
    public async Task OnlyRealPrototypesWithASpriteAreAvatars()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var client = pair.Client;
        var manifest = server.System<WFManifestSystem>();
        var store = server.System<WFAdminObserverStore>();
        var map = await pair.CreateTestMap();
        var session = await AdminKitFixture.Admin(pair);
        var observer = await AdminKitFixture.Enter(pair, session, map.GridCoords);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(manifest.IsAvatarPrototype(Avatar));
                Assert.That(manifest.IsAvatarPrototype(NoSprite), "The server reads no sprites; the client refuses this one.");
                Assert.That(manifest.IsAvatarPrototype(Abstract), Is.False);
                Assert.That(manifest.IsAvatarPrototype("WFAvatarTestMissing"), Is.False);
                Assert.That(manifest.IsAvatarPrototype(null), Is.False);
            });

            // Saving drops the avatars that could never be shown.
            store.Change(session, data =>
            {
                data.Presets.Add(new WFAvatarPreset { Name = "tool", Kind = WFAvatarKind.Object, Prototype = Avatar, EntryStyle = WFEntryStyle.Silent });
                data.Presets.Add(new WFAvatarPreset { Name = "abstract", Kind = WFAvatarKind.Object, Prototype = Abstract });
                data.Presets.Add(new WFAvatarPreset { Name = "missing", Kind = WFAvatarKind.Mob, Prototype = "WFAvatarTestMissing" });
                data.Presets.Add(new WFAvatarPreset
                {
                    Name = "named",
                    Kind = WFAvatarKind.Object,
                    Prototype = Avatar,
                    DisplayName = "Inspector Pry",
                    Description = "Here to look at the floor.",
                    EntryStyle = WFEntryStyle.Silent,
                });
            });
            var kept = store.Get(session)!.Presets.Select(p => p.Name).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(kept, Does.Contain("tool").And.Contain("named"));
                Assert.That(kept, Does.Not.Contain("abstract").And.Not.Contain("missing"));
            });
            Assert.That(manifest.TryManifest(session, "abstract", out _), Is.False);
        });

        await client.WaitAssertion(() =>
        {
            var avatars = client.System<WFAvatarVisualsSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(avatars.CanShow(Avatar));
                Assert.That(avatars.CanShow(NoSprite), Is.False, "No sprite, nothing to show.");
                Assert.That(avatars.CanShow(Abstract), Is.False);
                Assert.That(avatars.CanShow("WFAvatarTestMissing"), Is.False);
            });
        });

        // The avatar takes the prototype's name and description, and a marker that cannot be missed.
        var before = string.Empty;
        await server.WaitAssertion(() =>
        {
            before = server.EntMan.GetComponent<MetaDataComponent>(observer).EntityName;
            var prototype = server.ProtoMan.Index<EntityPrototype>(Avatar);
            Assert.That(manifest.TryManifest(session, "tool", out var message), message);

            var meta = server.EntMan.GetComponent<MetaDataComponent>(observer);
            var marker = server.EntMan.GetComponent<WFStaffMarkerComponent>(observer);
            Assert.Multiple(() =>
            {
                Assert.That(meta.EntityName, Is.EqualTo(prototype.Name));
                Assert.That(meta.EntityDescription, Is.EqualTo(prototype.Description));
                Assert.That(marker.Visuals, Is.EqualTo(WFMarkerVisuals.Nameplate | WFStaffMarkerSystem.ObjectDefault));
                Assert.That(WFStaffMarkerSystem.VisualsFor(WFAvatarKind.Mob, WFMarkerVisuals.Sigil | WFMarkerVisuals.Aura),
                    Is.EqualTo(WFMarkerVisuals.Sigil | WFMarkerVisuals.Aura), "A visual on the avatar of the admin's own choosing stands.");
                Assert.That(WFStaffMarkerSystem.VisualsFor(WFAvatarKind.Ghost, WFMarkerVisuals.Nameplate), Is.EqualTo(WFMarkerVisuals.Nameplate));
            });
        });
        await pair.RunTicksSync(10);

        var netObserver = server.EntMan.GetNetEntity(observer);
        await client.WaitAssertion(() =>
        {
            var sprite = client.EntMan.GetComponent<SpriteComponent>(client.EntMan.GetEntity(netObserver));
            Assert.Multiple(() =>
            {
                Assert.That(sprite.BaseRSI?.Path.ToString(), Does.Contain("crowbar.rsi"), "The observer is drawn as the crowbar.");
                Assert.That(sprite.LayerMapTryGet("ghostVariant", out _), Is.False);
                Assert.That(sprite.LayerMapTryGet("back-0", out _), Is.False, "The observer's own bag is not drawn on the avatar.");
            });
        });

        // A preset's own name and description win; hiding gives the observer its own back, and its own sprite.
        await server.WaitAssertion(() =>
        {
            Assert.That(manifest.TryManifest(session, "named", out var message), message);
            var meta = server.EntMan.GetComponent<MetaDataComponent>(observer);
            Assert.Multiple(() =>
            {
                Assert.That(meta.EntityName, Is.EqualTo("Inspector Pry"));
                Assert.That(meta.EntityDescription, Is.EqualTo("Here to look at the floor."));
            });

            Assert.That(manifest.TryUnmanifest(session, out _));
            Assert.That(server.EntMan.GetComponent<MetaDataComponent>(observer).EntityName, Is.EqualTo(before));
        });
        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var sprite = client.EntMan.GetComponent<SpriteComponent>(client.EntMan.GetEntity(netObserver));
            Assert.Multiple(() =>
            {
                Assert.That(sprite.BaseRSI?.Path.ToString(), Does.Contain("ghost_human.rsi"), "Hidden, it is a ghost again.");
                Assert.That(sprite.LayerMapTryGet("ghostVariant", out _));
                Assert.That(sprite.LayerMapTryGet("back-0", out _), "Its bag is drawn on it again.");
                Assert.That(sprite.Visible);
            });
        });

        await pair.CleanReturnAsync();
    }
}
