#nullable enable
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._WF.WornStorage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WF.WornStorage;

/// <summary>
/// With <see cref="WornStorageCVars.ClickOpens"/> on, clicking a worn backpack opens it, a held item still goes inside,
/// and dropping it on an empty hand takes it off into that hand. Off, a click takes it off as before.
/// </summary>
[TestOf(typeof(InventoryUIController))]
public sealed class WornStorageClickTest : InteractionTest
{
    private const string Slot = "back";

    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task ClickOpensAndDragTakesOff()
    {
        var inventory = SEntMan.System<InventorySystem>();
        var hands = SEntMan.System<SharedHandsSystem>();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var cfg = Client.ResolveDependency<IConfigurationManager>();
        EntityUid backpack = default;

        await Server.WaitPost(() =>
        {
            backpack = SEntMan.SpawnEntity("ClothingBackpack", SEntMan.GetCoordinates(PlayerCoords));
            Assert.That(inventory.TryEquip(SPlayer, backpack, Slot, silent: true, force: true), Is.True);
        });
        await RunTicks(5);

        try
        {
            // Option on, empty hand: the click opens the backpack and leaves it worn.
            await Client.WaitPost(() => cfg.SetCVar(WornStorageCVars.ClickOpens, true));
            await ClickBackSlot();
            await RunTicks(5);
            await Server.WaitAssertion(() =>
            {
                Assert.That(ui.IsUiOpen(backpack, StorageComponent.StorageUiKey.Key, SPlayer), Is.True,
                    "The click should open the worn backpack.");
                Assert.That(inventory.TryGetSlotEntity(SPlayer, Slot, out var worn) && worn == backpack, Is.True,
                    "The click shouldn't take the backpack off.");
            });

            // Option on, item in hand: the click still puts the item inside.
            EntityUid crowbar = default;
            await Server.WaitPost(() =>
            {
                crowbar = SEntMan.SpawnEntity("Crowbar", SEntMan.GetCoordinates(PlayerCoords));
                Assert.That(hands.TryPickupAnyHand(SPlayer, crowbar), Is.True);
            });
            await RunTicks(5);
            await ClickBackSlot();
            await RunTicks(5);
            await Server.WaitAssertion(() =>
            {
                var container = SEntMan.System<SharedContainerSystem>();
                Assert.That(container.TryGetContainingContainer(crowbar, out var holder) && holder.Owner == backpack,
                    Is.True, "A click with an item in hand should put it in the backpack.");
            });

            // Dropping on the inactive empty hand takes it off into that hand and makes it active. The fixture leaves
            // the player one hand, so add a second.
            const string target = "wf_worn_storage_test_hand";
            await Server.WaitPost(() => hands.AddHand(SPlayer, target, HandLocation.Left));
            await RunTicks(5);
            await Client.WaitPost(() =>
                Client.ResolveDependency<IUserInterfaceManager>().GetUIController<InventoryUIController>()
                    .DropWornStorageOnHand(Slot, target));
            await RunTicks(10);
            await Server.WaitAssertion(() =>
            {
                var comp = SEntMan.GetComponent<HandsComponent>(SPlayer);
                Assert.That(comp.Hands[target].HeldEntity, Is.EqualTo(backpack), "The backpack should land in the dropped-on hand.");
                Assert.That(comp.ActiveHand?.Name, Is.EqualTo(target));
                Assert.That(inventory.TryGetSlotEntity(SPlayer, Slot, out _), Is.False);
            });

            // Option off: a click takes it off into the active hand, as upstream does.
            await Server.WaitPost(() =>
            {
                hands.TryDrop(SPlayer, backpack);
                Assert.That(inventory.TryEquip(SPlayer, backpack, Slot, silent: true, force: true), Is.True);
            });
            await Client.WaitPost(() => cfg.SetCVar(WornStorageCVars.ClickOpens, false));
            await RunTicks(5);
            await ClickBackSlot();
            await RunTicks(10);
            await Server.WaitAssertion(() =>
            {
                Assert.That(inventory.TryGetSlotEntity(SPlayer, Slot, out _), Is.False,
                    "With the option off a click should take the backpack off.");
                Assert.That(hands.IsHolding(SPlayer, backpack), Is.True);
            });
        }
        finally
        {
            await Client.WaitPost(() => cfg.SetCVar(WornStorageCVars.ClickOpens, false));
        }
    }

    private async Task ClickBackSlot()
    {
        var button = GetControlFromChildren<SlotControl>(slot => slot.SlotName == Slot, GetWidget<HotbarGui>());
        await ClickControl(button.ButtonRect);
    }
}
