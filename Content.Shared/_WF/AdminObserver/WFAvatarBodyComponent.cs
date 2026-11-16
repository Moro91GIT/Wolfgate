namespace Content.Shared._WF.AdminObserver;

/// <summary>
/// On the dressed humanoid an admin observer carries to be drawn as a character avatar. It has no mind, sits
/// paused in a container on the observer, cannot be hurt and is deleted with it. Nothing should treat it as a person.
/// </summary>
[RegisterComponent]
public sealed partial class WFAvatarBodyComponent : Component
{
    /// <summary>The observer that carries this body.</summary>
    [ViewVariables]
    public EntityUid Observer;

    /// <summary>What the body was built from: the preset's name, character and outfit. A different one means a rebuild.</summary>
    [ViewVariables]
    public string Built = string.Empty;
}
