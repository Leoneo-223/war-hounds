using Godot;
using Godot.Collections;

// Dev tools: press F1-F9 to spawn the matching part as a pickup.
public partial class DevKit : Node
{
    [Export] public Array<PartData> Parts = new();
    [Export] public PackedScene PickupScene;
    [Export] public Node3D SpawnPoint;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            int index = (int)key.Keycode - (int)Key.F1;

            if (index >= 0 && index < Parts.Count)
            {
                SpawnPart(Parts[index]);
            }
        }
    }

    private void SpawnPart(PartData data)
    {
        var pickup = PickupScene.Instantiate<ItemPickup>();
        pickup.Item = data;
        GetTree().CurrentScene.AddChild(pickup);
        pickup.GlobalPosition = SpawnPoint.GlobalPosition;
    }
}