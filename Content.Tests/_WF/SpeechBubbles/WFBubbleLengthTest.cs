using System.Linq;
using Content.Client._WF.SpeechBubbles;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Content.Tests._WF.SpeechBubbles;

/// <summary>Speech bubble text is cut to length with an ellipsis, and the markup stays balanced.</summary>
[TestFixture]
[TestOf(typeof(WFBubbleLength))]
public sealed class WFBubbleLengthTest
{
    [Test]
    public void ShortMessageIsUnchanged()
    {
        var msg = FormattedMessage.FromMarkupOrThrow("hello [color=red]there[/color]");
        Assert.That(WFBubbleLength.Truncate(msg, 50), Is.SameAs(msg));
    }

    [Test]
    public void LongMessageIsCut()
    {
        var msg = FormattedMessage.FromMarkupOrThrow("abcdefghij klmnopqrst uvwxyz");
        var cut = WFBubbleLength.Truncate(msg, 10);
        Assert.That(cut.ToString(), Is.EqualTo("abcdefghij…"));
    }

    [Test]
    public void CutInsideATagClosesIt()
    {
        var msg = FormattedMessage.FromMarkupOrThrow("say [color=red]a very long red sentence[/color] and more");
        var cut = WFBubbleLength.Truncate(msg, 10);

        Assert.That(cut.ToString(), Is.EqualTo("say a very…"));
        var opened = cut.Nodes.Count(n => n.Name != null && !n.Closing);
        var closed = cut.Nodes.Count(n => n.Name != null && n.Closing);
        Assert.That(closed, Is.EqualTo(opened), "Every tag the cut leaves open must be closed.");
        Assert.That(cut.ToMarkup(), Does.EndWith("[/color]"));
    }
}
