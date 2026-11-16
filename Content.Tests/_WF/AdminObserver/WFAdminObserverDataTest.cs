using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server._WF.AdminObserver;
using Content.Shared._WF.AdminObserver;
using NUnit.Framework;

namespace Content.Tests._WF.AdminObserver;

/// <summary>Checks an admin's observer document survives storage and is cleaned up on the way in.</summary>
[TestFixture]
[TestOf(typeof(WFAdminObserverSerializer))]
public sealed class WFAdminObserverDataTest
{
    private static readonly HashSet<string> Entities = new() { "Bag", "Pouch", "Wrench", "Crate", "Corgi" };
    private static readonly HashSet<string> Gear = new() { "Suit" };

    private static WFAdminObserverData Clean(WFAdminObserverData data)
    {
        return WFAdminObserverSerializer.Sanitize(data, Entities.Contains, Gear.Contains);
    }

    private static WFKitNode Node(string prototype, params WFKitStored[] contents)
    {
        return new WFKitNode { Prototype = prototype, Contents = contents.ToList() };
    }

    private static WFKitStored At(int x, int y, int rotation, WFKitNode node)
    {
        return new WFKitStored { Node = node, X = x, Y = y, Rotation = rotation };
    }

    [Test]
    public void RoundTripKeepsEverything()
    {
        var data = new WFAdminObserverData
        {
            DefaultPreset = "Dog",
            Presets = new()
            {
                new WFAvatarPreset { Name = "Dog", Kind = WFAvatarKind.Mob, Prototype = "Corgi", DisplayName = "Ian" },
                new WFAvatarPreset
                {
                    Name = "Me",
                    Kind = WFAvatarKind.Character,
                    CharacterSlot = 2,
                    Outfit = "Suit",
                    EntryStyle = WFEntryStyle.Silent,
                    Description = "Looks official.",
                },
            },
            Marker = new WFStaffMarkerSettings
            {
                Wording = WFMarkerWording.RankAndName,
                Visuals = WFMarkerVisuals.Nameplate | WFMarkerVisuals.Ring,
                Ring = "placement",
                Color = "#fe019a",
            },
            Kit = new WFKitLayout
            {
                Slots = new() { ["back"] = Node("Bag", At(1, 2, 90, Node("Pouch", At(0, 0, 0, Node("Wrench"))))) },
                Hands = new() { ["left"] = Node("Wrench") },
            },
        };

        Assert.That(WFAdminObserverSerializer.TrySerialize(Clean(data), out var json));
        var read = WFAdminObserverSerializer.Deserialize(json);
        Assert.That(read, Is.Not.Null);
        Clean(read!);

        Assert.That(WFAdminObserverSerializer.TrySerialize(read!, out var again));
        Assert.That(again, Is.EqualTo(json), "A stored document should read back the same.");

        Assert.Multiple(() =>
        {
            Assert.That(read!.Version, Is.EqualTo(WFAdminObserverData.CurrentVersion));
            Assert.That(read.DefaultPreset, Is.EqualTo("Dog"));
            Assert.That(read.Presets.Select(p => p.Name), Is.EqualTo(new[] { "Dog", "Me" }));
            Assert.That(read.Presets[1].CharacterSlot, Is.EqualTo(2));
            Assert.That(read.Presets[1].EntryStyle, Is.EqualTo(WFEntryStyle.Silent));
            Assert.That(read.Marker.Visuals, Is.EqualTo(WFMarkerVisuals.Nameplate | WFMarkerVisuals.Ring));
            Assert.That(read.Marker.Color, Is.EqualTo("#fe019a"));

            var stored = read.Kit!.Slots["back"].Contents.Single();
            Assert.That((stored.X, stored.Y, stored.Rotation), Is.EqualTo((1, 2, 90)));
            Assert.That(stored.Node.Contents.Single().Node.Prototype, Is.EqualTo("Wrench"));
            Assert.That(read.Kit.Hands["left"].Prototype, Is.EqualTo("Wrench"));
        });
    }

    [Test]
    public void VersionIsWrittenAndNewerOnesRefused()
    {
        Assert.That(WFAdminObserverSerializer.TrySerialize(new WFAdminObserverData(), out var json));
        Assert.That(json, Does.Contain($"\"Version\":{WFAdminObserverData.CurrentVersion}"));

        var newer = json.Replace($"\"Version\":{WFAdminObserverData.CurrentVersion}", "\"Version\":999");
        Assert.Multiple(() =>
        {
            Assert.That(WFAdminObserverSerializer.Deserialize(newer), Is.Null, "A newer document should not be read.");
            Assert.That(WFAdminObserverSerializer.Deserialize("{ not json"), Is.Null);
        });
    }

    [Test]
    public void UnknownPrototypesAreDropped()
    {
        var data = Clean(new WFAdminObserverData
        {
            DefaultPreset = "Gone",
            Presets = new()
            {
                new WFAvatarPreset { Name = "Gone", Kind = WFAvatarKind.Object, Prototype = "Removed" },
                new WFAvatarPreset { Name = "Kept", Kind = WFAvatarKind.Object, Prototype = "Crate" },
                new WFAvatarPreset { Name = "Me", Kind = WFAvatarKind.Character, Outfit = "RemovedGear" },
            },
            Kit = new WFKitLayout
            {
                Slots = new()
                {
                    ["back"] = Node("Bag", At(0, 0, 0, Node("Removed", At(0, 0, 0, Node("Wrench")))), At(2, 0, 0, Node("Wrench"))),
                    ["belt"] = Node("Removed"),
                },
            },
        });

        Assert.Multiple(() =>
        {
            Assert.That(data.Presets.Select(p => p.Name), Is.EqualTo(new[] { "Kept", "Me" }));
            Assert.That(data.DefaultPreset, Is.Null, "A default that names a dropped preset should clear.");
            Assert.That(data.Presets[1].Outfit, Is.Null, "A missing outfit should clear, keeping the character.");
            Assert.That(data.Kit!.Slots.Keys, Is.EqualTo(new[] { "back" }));
            Assert.That(data.Kit.Slots["back"].Contents.Select(c => c.Node.Prototype), Is.EqualTo(new[] { "Wrench" }));
        });
    }

    [Test]
    public void LimitsHold()
    {
        var deep = Node("Bag", At(0, 0, 0, Node("Bag", At(0, 0, 0, Node("Bag", At(0, 0, 0, Node("Wrench")))))));
        var wide = Node("Bag", Enumerable.Range(0, 300).Select(i => At(i, 0, 0, Node("Wrench"))).ToArray());
        var data = Clean(new WFAdminObserverData
        {
            Presets = Enumerable.Range(0, 50)
                .Select(i => new WFAvatarPreset { Name = $"Preset {i}", DisplayName = new string('x', 500) })
                .Append(new WFAvatarPreset { Name = "Preset 0" })
                .ToList(),
            Marker = new WFStaffMarkerSettings { Visuals = WFMarkerVisuals.None, Color = "pink" },
            Kit = new WFKitLayout { Slots = new() { ["back"] = deep, ["belt"] = wide } },
        });

        Assert.Multiple(() =>
        {
            Assert.That(data.Presets, Has.Count.EqualTo(WFAdminObserverData.MaxPresets));
            Assert.That(data.Presets[0].DisplayName, Has.Length.EqualTo(WFAdminObserverData.MaxDisplayNameLength));
            Assert.That(data.Marker.Visuals, Is.EqualTo(WFMarkerVisuals.Nameplate), "A marker always has a visual.");
            Assert.That(data.Marker.Color, Is.Null, "A colour that is not hex should clear.");

            var third = data.Kit!.Slots["back"].Contents.Single().Node.Contents.Single().Node;
            Assert.That(third.Contents, Is.Empty, "Nothing is kept below the third level.");
            Assert.That(Count(data.Kit), Is.EqualTo(WFAdminObserverData.MaxKitNodes));
        });
    }

    [Test]
    public void OversizedDocumentIsRefused()
    {
        var data = new WFAdminObserverData
        {
            Presets = Enumerable.Range(0, WFAdminObserverData.MaxPresets)
                .Select(i => new WFAvatarPreset
                {
                    Name = $"Preset {i}",
                    Description = new string('x', WFAdminObserverData.MaxDescriptionLength),
                    OutfitLayout = new WFKitLayout
                    {
                        Slots = new()
                        {
                            ["back"] = Node("Bag", Enumerable.Range(0, 150).Select(n => At(n, 0, 0, Node("Wrench"))).ToArray()),
                        },
                    },
                    Kind = WFAvatarKind.Character,
                })
                .ToList(),
        };

        Assert.That(WFAdminObserverSerializer.TrySerialize(Clean(data), out _), Is.False);
    }

    private static int Count(WFKitLayout layout)
    {
        return layout.Slots.Values.Concat(layout.Hands.Values).Sum(Count);
    }

    private static int Count(WFKitNode node)
    {
        return 1 + node.Contents.Sum(c => Count(c.Node));
    }
}
