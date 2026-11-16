#nullable enable
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Maths;
using Content.Client._WF.CharacterPreview;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests._WF.CharacterPreview;

/// <summary>
/// The preview frame turns once per drag step, clamps its zoom, lays the sprite out at that zoom, and every floor tile
/// it offers loads.
/// </summary>
[TestFixture]
[TestOf(typeof(WFPreviewFrame))]
public sealed class PreviewFrameTest
{
    [Test]
    public async Task DragZoomAndBackgrounds()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var frame = new WFPreviewFrame { SetSize = new Vector2(200, 200) };
            var view = new SpriteView();
            frame.AddChild(view);

            var turns = new List<int>();
            frame.Turned += turns.Add;
            frame.Drag(WFPreviewFrame.TurnDistance * 2.5f);
            frame.Drag(-WFPreviewFrame.TurnDistance * 1.5f);
            // 2.5 steps right turns twice and keeps half a step; 1.5 back nets one step left.
            Assert.That(turns, Is.EqualTo(new[] { 1, 1, -1 }), "One turn per full step of drag, either way.");

            frame.SetZoom(10);
            Assert.That(frame.Zoom, Is.EqualTo(WFPreviewFrame.MaxZoom));
            frame.SetZoom(0);
            Assert.That(frame.Zoom, Is.EqualTo(WFPreviewFrame.MinZoom));

            frame.SetZoom(2);
            frame.Measure(new Vector2(200, 200));
            frame.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(200, 200)));
            Assert.That(view.Size.X, Is.EqualTo(400).Within(0.5f), "At 2x the sprite is laid out twice the box.");

            for (var i = 0; i <= WFPreviewFrame.Backgrounds.Length; i++)
            {
                Assert.DoesNotThrow(() => frame.SetBackground(i), $"Background {i} should load.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
