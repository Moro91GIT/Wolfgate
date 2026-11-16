#nullable enable
using Content.Server._WF.AdminObserver;
using Content.Server.Administration.Managers;
using Content.Shared._WF.AdminObserver;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WF.AdminObserver;

/// <summary>Only an admin, in an admin observer, can manifest; and what they show never carries their account name.</summary>
[TestFixture]
[TestOf(typeof(WFManifestSystem))]
public sealed class ManifestPermissionTest
{
    [Test]
    public async Task OnlyAnAdminInAnObserverManifests()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var manifest = server.System<WFManifestSystem>();
        var admins = server.ResolveDependency<IAdminManager>();
        var map = await pair.CreateTestMap();

        var session = await AdminKitFixture.Admin(pair);

        // An admin in a body is not an observer.
        var human = await AdminKitFixture.Living(pair, map.GridCoords);
        await server.WaitAssertion(() =>
        {
            Assert.That(manifest.TryManifest(session, null, out _), Is.False);
            Assert.That(entMan.HasComponent<WFManifestedComponent>(human), Is.False);
        });

        // A preset they don't have is refused; none at all is the plain ghost.
        var observer = await AdminKitFixture.Enter(pair, session, map.GridCoords);
        await server.WaitAssertion(() =>
        {
            Assert.That(manifest.TryManifest(session, "No Such Preset", out _), Is.False);
            Assert.That(entMan.HasComponent<WFManifestedComponent>(observer), Is.False);
            Assert.That(manifest.TryManifest(session, null, out _));
            Assert.That(manifest.TryUnmanifest(session, out _));
        });

        // Without the flag the same observer stays hidden.
        await server.WaitPost(() => admins.DeAdmin(session));
        await pair.RunTicksSync(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(manifest.TryManifest(session, null, out _), Is.False);
            Assert.That(entMan.HasComponent<WFManifestedComponent>(observer), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AccountNameStaysOutUnlessAskedFor()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;
        var entMan = server.EntMan;
        var manifest = server.System<WFManifestSystem>();
        var store = server.System<WFAdminObserverStore>();
        var metaData = server.System<MetaDataSystem>();
        var map = await pair.CreateTestMap();

        var session = await AdminKitFixture.Admin(pair);
        var observer = await AdminKitFixture.Enter(pair, session, map.GridCoords);

        // aghost names a bodiless admin's observer after their account; manifesting must not show that.
        await server.WaitAssertion(() =>
        {
            metaData.SetEntityName(observer, session.Name);
            Assert.That(manifest.TryManifest(session, null, out _));

            var marker = entMan.GetComponent<WFStaffMarkerComponent>(observer);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<MetaDataComponent>(observer).EntityName, Does.Not.Contain(session.Name));
                Assert.That(marker.Label, Does.Not.Contain(session.Name), "The default wording is the rank.");
            });

            Assert.That(manifest.TryUnmanifest(session, out _));
            Assert.That(entMan.GetComponent<MetaDataComponent>(observer).EntityName, Is.EqualTo(session.Name),
                "Hiding puts the old name back.");

            // A named preset shows its name; the account name appears only with that wording.
            Assert.That(store.Change(session, data =>
            {
                data.Presets.Add(new WFAvatarPreset { Name = "Inspector", DisplayName = "The Inspector", Description = "Here to help." });
                data.DefaultPreset = "Inspector";
                data.Marker.Wording = WFMarkerWording.Name;
            }));
            Assert.That(manifest.TryManifest(session, null, out _));

            var meta = entMan.GetComponent<MetaDataComponent>(observer);
            Assert.Multiple(() =>
            {
                Assert.That(meta.EntityName, Is.EqualTo("The Inspector"));
                Assert.That(meta.EntityDescription, Is.EqualTo("Here to help."));
                Assert.That(entMan.GetComponent<WFStaffMarkerComponent>(observer).Label, Is.EqualTo(session.Name));
            });
        });

        await pair.CleanReturnAsync();
    }
}
