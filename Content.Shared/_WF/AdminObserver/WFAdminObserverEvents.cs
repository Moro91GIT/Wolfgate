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

/// <summary>Client to server: save what my observer carries as my kit layout now.</summary>
[Serializable, NetSerializable]
public sealed class WFAdminKitSaveRequestEvent : EntityEventArgs;

/// <summary>Server to client: kit items that had nowhere to go, by entity prototype.</summary>
[Serializable, NetSerializable]
public sealed class WFAdminKitUnplacedEvent : EntityEventArgs
{
    public List<string> Prototypes = new();

    /// <summary>Whether this is a new observer's whole list, replacing what the client was showing.</summary>
    public bool Replace;
}

/// <summary>Server to client: open the Kit window.</summary>
[Serializable, NetSerializable]
public sealed class WFAdminKitOpenEvent : EntityEventArgs;

/// <summary>Client to server: open the strip window of my character avatar's body, so I can dress it.</summary>
[Serializable, NetSerializable]
public sealed class WFAvatarDressRequestEvent : EntityEventArgs
{
    /// <summary>The name of the character preset to dress.</summary>
    public string Preset = string.Empty;
}

/// <summary>Client to server: save what my character avatar's body wears into its preset.</summary>
[Serializable, NetSerializable]
public sealed class WFAvatarSaveOutfitRequestEvent : EntityEventArgs
{
    /// <inheritdoc cref="WFAvatarDressRequestEvent.Preset"/>
    public string Preset = string.Empty;
}
