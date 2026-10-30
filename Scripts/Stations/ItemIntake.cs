using Godot;

// Attach to an Area3D that is a direct child of a Station. It is the "mouth" of the machine:
// any dropped or thrown item that touches it is offered to the station.
// If the station takes it, the world item shrinks or disappears.
// If the station refuses it, the item just stays where it is.
//
// Area3D settings: Collision Layer = nothing, Collision Mask = layer 2 only
// (that's where ItemPickup puts itself).
public partial class ItemIntake : Area3D
{
    private Station _station;
    private float _timer;

    public override void _Ready()
    {
        _station = GetParentOrNull<Station>();
        if (_station == null)
            GD.PushError("ItemIntake must be a direct child of a Station node.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_station == null) return;

        // Check a few times per second, not every frame. Polling (instead of BodyEntered)
        // also catches items that were refused earlier and are still sitting here.
        _timer += (float)delta;
        if (_timer < 0.2f) return;
        _timer = 0f;

        foreach (Node3D body in GetOverlappingBodies())
        {
            if (body is not ItemPickup pickup) continue;
            if (pickup.IsQueuedForDeletion() || pickup.Item == null) continue;

            int taken = _station.Accept(pickup.Item, pickup.Count);
            if (taken <= 0) continue;

            pickup.Count -= taken;
            if (pickup.Count <= 0) pickup.QueueFree();
        }
    }
}