using Godot;
using System.Collections.Generic;

public enum LocationType { Other, Home, Work, Social, Outdoor }

[GlobalClass]
public partial class LocationMarker : Marker3D
{
    [Export] public string LocationId { get; set; } = "";
    [Export] public LocationType Type { get; set; } = LocationType.Other;

    // NPCs currently "inside" this building (filled in by NPC.cs). Used by HomeDoor.
    public readonly List<NPC> Inside = new();

    // NPCs vanish into the building when they arrive at a Home marker.
    public bool HidesNpc => Type == LocationType.Home;

    // Yaw (radians) an NPC should have to look along this marker's forward (-Z).
    public float FacingYaw
    {
        get
        {
            Vector3 fwd = -GlobalBasis.Z;
            return Mathf.Atan2(-fwd.X, -fwd.Z);
        }
    }

    private static readonly Dictionary<string, LocationMarker> All = new();

    public static LocationMarker Find(string id) =>
        All.TryGetValue(id, out var m) ? m : null;

    public override void _EnterTree()
    {
        if (LocationId != "") All[LocationId] = this;
    }

    public override void _ExitTree()
    {
        if (All.TryGetValue(LocationId, out var m) && m == this) All.Remove(LocationId);
    }
}