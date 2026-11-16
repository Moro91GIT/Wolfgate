using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.JudgeLine;

/// <summary>The nature of an emergency a vagrant calls the judges to.</summary>
[Serializable, NetSerializable]
public enum WFJudgeLineKind : byte
{
    /// <summary>The caller's ship is being shot at by another ship.</summary>
    UnderFire,

    /// <summary>Hostiles are aboard the caller's ship.</summary>
    Boarded,

    /// <summary>The caller or a crewmate is badly hurt, critical or dead.</summary>
    Medical,

    /// <summary>The caller's ship has no thrust.</summary>
    Stranded,

    /// <summary>The caller is in space with no ship of their own.</summary>
    Adrift,

    /// <summary>The caller is attacked in person.</summary>
    Assault,
}

/// <summary>Where a call stands, as the caller sees it.</summary>
[Serializable, NetSerializable]
public enum WFJudgeLinePhase : byte
{
    None,
    Queued,
    Dispatched,
    OnScene,
    Leaving,
}

/// <summary>What the Judge Line app shows its holder.</summary>
[Serializable, NetSerializable]
public sealed class WFJudgeLineUiState : BoundUserInterfaceState
{
    /// <summary>Why the holder can't call now, or null if they can.</summary>
    public string? Refusal;

    public int Fee;
    public WFJudgeLinePhase Phase;
    public WFJudgeLineKind Kind;

    /// <summary>Seconds until the responder arrives, while dispatched.</summary>
    public int Eta;

    /// <summary>Place in the queue, while queued.</summary>
    public int Queue;

    /// <summary>The latest line from the responder.</summary>
    public string? Status;
}

/// <summary>Places a call of a kind from the Judge Line app.</summary>
[Serializable, NetSerializable]
public sealed class WFJudgeLineCallMessage(WFJudgeLineKind kind) : CartridgeMessageEvent
{
    public readonly WFJudgeLineKind Kind = kind;
}

/// <summary>Cancels the holder's call from the Judge Line app.</summary>
[Serializable, NetSerializable]
public sealed class WFJudgeLineCancelMessage : CartridgeMessageEvent;
