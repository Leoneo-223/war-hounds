using Godot;

// Lives as a plain Node on the Player. Holds the items, handles selection,
// dropping, and shows the selected item's model at HoldPoint.
public partial class Inventory : Node
{
    // Global access so pickups and stations can reach the player's inventory
    // without needing node paths. Fine for a single-player game.
    public static Inventory Instance { get; private set; }

    // Fired whenever contents or selection change. The hotbar listens to this.
    [Signal] public delegate void ChangedEventHandler();

    [Export] public int SlotCount = 10;

    // Marker3D under the camera where the held item appears.
    [Export] public Marker3D HoldPoint;

    // Pickup.tscn, spawned when you drop something.
    [Export] public PackedScene PickupScene;

    // Parallel arrays: slot i holds Items[i] with Counts[i] of them.
    // A null item means the slot is empty.
    public ItemData[] Items;
    public int[] Counts;

    public int Selected { get; private set; }
    public ItemData SelectedItem => Items[Selected];

    public override void _Ready()
    {
        Instance = this;
        Items = new ItemData[SlotCount];
        Counts = new int[SlotCount];
        Refresh();
    }

    // Tries to add items. Returns how many did NOT fit (0 = all added).
    public int Add(ItemData item, int count)
    {
        // Pass 1: top up existing stacks of the same item.
        for (int i = 0; i < SlotCount && count > 0; i++)
        {
            if (Items[i] != item) continue;
            int moved = Mathf.Min(item.MaxStack - Counts[i], count);
            Counts[i] += moved;
            count -= moved;
        }

        // Pass 2: fill empty slots with new stacks.
        for (int i = 0; i < SlotCount && count > 0; i++)
        {
            if (Items[i] != null) continue;
            int moved = Mathf.Min(item.MaxStack, count);
            Items[i] = item;
            Counts[i] = moved;
            count -= moved;
        }

        Refresh();
        return count;
    }

    // Removes items from one slot and clears it when it reaches zero.
    // Stations will call this to consume ingredients.
    public void RemoveFromSlot(int slot, int count = 1)
    {
        if (Items[slot] == null) return;

        Counts[slot] -= count;
        if (Counts[slot] <= 0)
        {
            Items[slot] = null;
            Counts[slot] = 0;
        }
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

        RemoveFromSlot(Selected);
    }

    public override void _UnhandledInput(InputEvent e)
    {
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

    // Single place that updates everything after a change.
    private void Refresh()
    {
        UpdateHeldModel();
        EmitSignal(SignalName.Changed);
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