using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.Wilderness;

/// <summary>Every zone's edge on a map, for radars: maps reach every client, far grids may not.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WFZoneRingsComponent : Component
{
    [AutoNetworkedField]
    public List<WFZoneRing> Rings = new();
}

/// <summary>One zone's edge: a circle on the map.</summary>
[Serializable, NetSerializable]
public readonly record struct WFZoneRing(Vector2 Centre, float Radius, ProtoId<WFZonePrototype> Zone);
