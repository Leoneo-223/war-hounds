using Godot;
using System.Collections.Generic;

// Drop these in your world and give each a unique LocationId
// (matches HomeLocationId, ScheduleEntry.LocationId, etc.).
[GlobalClass]
public partial class LocationMarker : Marker3D
{
    [Export] public string LocationId { get; set; } = "";

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