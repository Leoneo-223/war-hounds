using Godot;

// The player's inventory. It is a Storage (arrays, Add, Remove...) plus
// slot selection, dropping, and showing the held item's model.
// Lives as a plain Node on the Player.
public partial class Inventory : Storage
{
    // Global access so pickups, crates and stations can reach the player's inventory
    // without needing node paths. Fine for a single-player game.
    public static Inventory Instance { get; private set; }

    // Marker3D under the camera where the held item appears.
    [Export] public Marker3D HoldPoint;

    // Pickup.tscn, spawned when you drop something.
    [Export] public PackedScene PickupScene;

    public int Selected { get; private set; }
    public ItemData SelectedItem => Items[Selected];

    public override void _Ready()
    {
        base._Ready();   // creates the Items/Counts arrays
        Instance = this;
        Refresh();
    }

    // Selects a slot. The modulo wraps around so scrolling loops 10 -> 1.
    public void Select(int slot)
    {
        Selected = (slot % SlotCount + SlotCount) % SlotCount;
        Refresh();
    }

    // Throws one item of the selected slot out in front of the camera.
    public void DropSelected()
    {
        var item = Items[Selected];
        if (item == null || PickupScene == null) return;

        var cam = GetViewport().GetCamera3D();

        // Set Item/Count BEFORE adding to the tree, because the pickup
        // reads Item in _Ready to spawn its model.
        var pickup = PickupScene.Instantiate<ItemPickup>();
        pickup.Item = item;
        pickup.Count = 1;

        GetTree().CurrentScene.AddChild(pickup);

        // Camera forward is -Z. Spawn slightly ahead and toss it.
        pickup.GlobalPosition = cam.GlobalPosition - cam.GlobalBasis.Z * 0.8f;
        pickup.LinearVelocity = -cam.GlobalBasis.Z * 3f;
        pickup.GlobalRotation = new Vector3(0, cam.GlobalRotation.Y, 0);

        RemoveFromSlot(Selected);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Same as in PlayerController.cs, if an UI is open, return
        if (GameState.UiOpen) return;

        if (e is InputEventKey { Pressed: true, Echo: false } key)
        {
            // Number keys: 1-9 select slots 0-8, key 0 selects slot 9.
            for (int i = 0; i < SlotCount; i++)
            {
                Key k = i < 9 ? (Key)((int)Key.Key1 + i) : Key.Key0;
                if (key.PhysicalKeycode == k) { Select(i); return; }
            }

            if (key.PhysicalKeycode == Key.Q) DropSelected();
        }

        // Scroll wheel cycles through slots.
        if (e is InputEventMouseButton { Pressed: true } mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp) Select(Selected - 1);
            if (mb.ButtonIndex == MouseButton.WheelDown) Select(Selected + 1);
        }
    }

    // Extends Storage's Refresh: also update the held model, then emit Changed.
    protected override void Refresh()
    {
        UpdateHeldModel();
        base.Refresh();
    }

    // Swaps the model in the player's hand to match the selected slot.
    private void UpdateHeldModel()
    {
        if (HoldPoint == null) return;

        foreach (Node child in HoldPoint.GetChildren())
            child.QueueFree();

        if (SelectedItem?.WorldModel != null)
            HoldPoint.AddChild(SelectedItem.WorldModel.Instantiate());
    }
}