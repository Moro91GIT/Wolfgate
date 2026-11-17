#nullable enable
using System.Linq;
using Content.Server._WF.AdminObserver;
using Content.Server.Administration.Managers;
using Content.Shared._WF.AdminObserver;
using Content.Shared.Administration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WF.AdminObserver;

/// <summary>What the observer panel asks for: saved at once for an admin, refused for anyone else.</summary>
[TestFixture]
[TestOf(typeof(WFObserverPanelSystem))]
public sealed class ObserverPanelRequestsTest
{
    [Test]
    public async Task RequestsAreSavedForAnAdminAndRefusedOtherwise()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var panel = server.System<WFObserverPanelSystem>();
        var store = server.System<WFAdminObserverStore>();
        var manifest = server.System<WFManifestSystem>();
        var admins = server.ResolveDependency<IAdminManager>();
        var map = await pair.CreateTestMap();
        var session = await AdminKitFixture.Admin(pair);
        var observer = await AdminKitFixture.Enter(pair, session, map.GridCoords);

        await server.WaitAssertion(() =>
        {
            WFAdminObserverData Saved() => store.Get(session)!;
            var stock = Saved().Presets.Count;

            // The staff marker.
            var marker = new WFStaffMarkerSettings
            {
                Wording = WFMarkerWording.Staff,
                Visuals = WFMarkerVisuals.Ring | WFMarkerVisuals.Glyph,
                Ring = "WFStaffRingBrackets",
            };
            Assert.That(panel.SetMarker(session, marker));
            Assert.Multiple(() =>
            {
                Assert.That(Saved().Marker.Wording, Is.EqualTo(WFMarkerWording.Staff));
                Assert.That(Saved().Marker.Visuals, Is.EqualTo(WFMarkerVisuals.Ring | WFMarkerVisuals.Glyph));
                Assert.That(Saved().Marker.Ring, Is.EqualTo("WFStaffRingBrackets"));
            });

            // New presets get a numbered name until they are given one.
            Assert.That(panel.SavePreset(session, null, new WFAvatarPreset()));
            Assert.That(panel.SavePreset(session, null, new WFAvatarPreset()));
            var added = Saved().Presets.Skip(stock).Select(p => p.Name).ToList();
            Assert.That(added, Has.Count.EqualTo(2));
            Assert.That(added[0], Is.Not.EqualTo(added[1]));

            // Editing one: it is known by the name players will see.
            var edited = new WFAvatarPreset { Kind = WFAvatarKind.Object, Prototype = "Crowbar", DisplayName = "Inspector" };
            Assert.That(panel.SavePreset(session, added[0], edited));
            var inspector = Saved().Presets.Single(p => p.Name == "Inspector");
            Assert.Multiple(() =>
            {
                Assert.That(inspector.Kind, Is.EqualTo(WFAvatarKind.Object));
                Assert.That(inspector.Prototype, Is.EqualTo("Crowbar"));
                Assert.That(Saved().Presets.Any(p => p.Name == added[0]), Is.False);
                Assert.That(panel.SavePreset(session, "Inspector", new WFAvatarPreset { Kind = WFAvatarKind.Mob, Prototype = "WFNoSuchEntity" }), Is.False,
                    "An avatar that could never be shown is not saved.");
                Assert.That(panel.SavePreset(session, "WFNoSuchPreset", new WFAvatarPreset()), Is.False);
            });

            // The default follows a rename and goes with a delete.
            Assert.That(panel.SetDefault(session, "Inspector"));
            Assert.That(panel.SetDefault(session, "WFNoSuchPreset"), Is.False);
            Assert.That(panel.SavePreset(session, "Inspector", new WFAvatarPreset { Kind = WFAvatarKind.Object, Prototype = "Crowbar", DisplayName = "Pry" }));
            Assert.That(Saved().DefaultPreset, Is.EqualTo("Pry"));
            Assert.That(panel.DeletePreset(session, "Pry"));
            Assert.Multiple(() =>
            {
                Assert.That(Saved().DefaultPreset, Is.Null);
                Assert.That(panel.DeletePreset(session, "Pry"), Is.False);
                Assert.That(Saved().Presets, Has.Count.EqualTo(stock + 1));
            });

            // A marker change shows at once on an admin who is manifested.
            Assert.That(manifest.TryManifest(session, null, out var message), message);
            Assert.That(panel.SetMarker(session, new WFStaffMarkerSettings { Visuals = WFMarkerVisuals.Aura, Aura = "WFStaffAuraOutline" }));
            var worn = entMan.GetComponent<WFStaffMarkerComponent>(observer);
            Assert.Multiple(() =>
            {
                Assert.That(worn.Visuals, Is.EqualTo(WFMarkerVisuals.Aura));
                Assert.That(worn.Aura, Is.EqualTo("WFStaffAuraOutline"));
                Assert.That(panel.OpenTool(session, WFObserverTool.CrewMonitor), "The observer's own consoles open from the panel.");
                Assert.That(panel.Open(session));
            });
        });
        await pair.RunTicksSync(5);

        // Without the Admin flag, nothing.
        await server.WaitPost(() => admins.DeAdmin(session));
        await AdminKitFixture.WaitFor(pair, () => !admins.HasAdminFlag(session, AdminFlags.Admin) && store.Get(session) == null,
            "The player never lost the flag.");

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(panel.SetMarker(session, new WFStaffMarkerSettings()), Is.False);
                Assert.That(panel.SavePreset(session, null, new WFAvatarPreset()), Is.False);
                Assert.That(panel.DeletePreset(session, "Ghost"), Is.False);
                Assert.That(panel.SetDefault(session, null), Is.False);
                Assert.That(panel.OpenTool(session, WFObserverTool.CrewMonitor), Is.False);
                Assert.That(panel.Open(session), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }
}
