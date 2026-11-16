using Content.Client._WF.SpeechBubbles;
using Content.Shared._WF.SpeechBubbles;

namespace Content.Client.Options.UI.Tabs;

/// <summary>The speech bubble toggles and length slider on the Misc tab.</summary>
public sealed partial class MiscTab
{
    private void WfAddSpeechBubbleOptions()
    {
        Control.AddOptionCheckBox(SpeechBubbleCVars.Enabled, SpeechBubblesCheckBox);
        Control.AddOptionCheckBox(SpeechBubbleCVars.NonMobs, SpeechBubblesNonMobsCheckBox);
        Control.AddOptionCheckBox(SpeechBubbleCVars.Ghosts, SpeechBubblesGhostsCheckBox);
        Control.AddOptionCheckBox(SpeechBubbleCVars.Emotes, SpeechBubblesEmotesCheckBox);
        Control.AddOptionSlider(
            SpeechBubbleCVars.MaxLength,
            SpeechBubbleMaxLengthSlider,
            0,
            300,
            (_, value) => value == 0
                ? Loc.GetString("ui-options-wf-speech-bubble-max-length-off")
                : Loc.GetString("ui-options-wf-speech-bubble-max-length-value",
                    ("length", Math.Max(value, WFBubbleLength.MinLength))));
    }
}
