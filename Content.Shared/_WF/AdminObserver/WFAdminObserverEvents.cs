using Robust.Shared.Serialization;

namespace Content.Shared._WF.AdminObserver;

/// <summary>
/// Server to client: the admin's own observer document, on load and after each accepted change. Null data tells a
/// client that is no longer an admin to forget it.
/// </summary>
[Serializable, NetSerializable]
public sealed class WFAdminObserverDataEvent : EntityEventArgs
{
    public WFAdminObserverData? Data;
}

/// <summary>Client to server: send my observer document. Ignored without the Admin flag.</summary>
[Serializable, NetSerializable]
public sealed class WFAdminObserverRequestDataEvent : EntityEventArgs;
