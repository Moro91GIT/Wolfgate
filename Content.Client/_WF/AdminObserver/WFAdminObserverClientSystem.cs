using Content.Shared._WF.AdminObserver;

namespace Content.Client._WF.AdminObserver;

/// <summary>Holds the local admin's observer document as the server last sent it.</summary>
public sealed partial class WFAdminObserverClientSystem : EntitySystem
{
    /// <summary>The local admin's document, or null when none has arrived or the player is not an admin.</summary>
    public WFAdminObserverData? Data { get; private set; }

    /// <summary>Raised after <see cref="Data"/> changes.</summary>
    public event Action? DataChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<WFAdminObserverDataEvent>(OnData);
    }

    /// <summary>Asks the server for the document again.</summary>
    public void Request()
    {
        RaiseNetworkEvent(new WFAdminObserverRequestDataEvent());
    }

    private void OnData(WFAdminObserverDataEvent ev)
    {
        Data = ev.Data;
        DataChanged?.Invoke();
    }
}
