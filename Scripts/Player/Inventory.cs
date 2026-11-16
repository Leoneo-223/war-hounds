using Godot;
using System.Collections.Generic;

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

    public ItemData SelectedItem
    {
        get
        {
            return Items[Selected];
        }
    }

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
        ItemData item = Items[Selected];
        if (item == null || PickupScene == null)
        {
            return;
        }

        Camera3D cam = GetViewport().GetCamera3D();

        // Set Item/Count BEFORE adding to the tree, because the pickup
        // reads Item in _Ready to spawn its model.
        ItemPickup pickup = PickupScene.Instantiate<ItemPickup>();
        pickup.Item = item;
        pickup.Count = 1;

        GetTree().CurrentScene.AddChild(pickup);

        // Camera forward is -Z. Spawn slightly ahead and toss it.
        pickup.GlobalPosition = cam.GlobalPosition - cam.GlobalBasis.Z * 0.8f;
        pickup.LinearVelocity = -cam.GlobalBasis.Z * 3f;
        pickup.GlobalRotation = new Vector3(0, cam.GlobalRotation.Y, 0);

        RemoveFromSlot(Selected);
    }

    // How many of this item the player has in total.
    public int GetTotalCount(ItemData item)
    {
        int total = 0;

        for (int i = 0; i < SlotCount; i++)
        {
            if (Items[i] == item)
            {
                total += Counts[i];
            }
        }

        return total;
    }

    // Takes one of this item out of the inventory. Returns false if there is none.
    public bool RemoveOne(ItemData item)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Items[i] == item)
            {
                RemoveFromSlot(i, 1);
                return true;
            }
        }

        return false;
    }

    // Swaps the item in the selected slot for another one (for example empty cup -> full cup).
    public bool ReplaceSelected(ItemData newItem)
    {
        if (Items[Selected] == null || newItem == null)
        {
            return false;
        }

        if (Counts[Selected] == 1)
        {
            Items[Selected] = newItem;
            Refresh();   // also swaps the model in your hand
            return true;
        }

        // A stack of cups: put the new item in another slot, then use up one cup.
        if (Add(newItem, 1) > 0)
        {
            return false;   // no room
        }

        RemoveFromSlot(Selected, 1);
        return true;
    }

    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        var result = new List<ItemData>();

        for (int i = 0; i < SlotCount; i++)
        {
            if (Items[i] != null && Items[i].Category == category)
            {
                result.Add(Items[i]);
            }
        }

        return result;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Same as in Player.cs: if a UI is open, do nothing
        if (GameState.UiOpen)
        {
            return;
        }

        InputEventKey key = e as InputEventKey;
        if (key != null && key.Pressed && !key.Echo)
        {
            // Number keys: 1-9 select slots 0-8, key 0 selects slot 9.
            for (int i = 0; i < SlotCount; i++)
            {
                Key k;
                if (i < 9)
                {
                    k = (Key)((int)Key.Key1 + i);
                }
                else
                {
                    k = Key.Key0;
                }

                if (key.PhysicalKeycode == k)
                {
                    Select(i);
                    return;
                }
            }

            if (key.PhysicalKeycode == Key.Q)
            {
                DropSelected();
            }
        }

        // Scroll wheel cycles through slots.
        InputEventMouseButton mb = e as InputEventMouseButton;
        if (mb != null && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp)
            {
                Select(Selected - 1);
            }

            if (mb.ButtonIndex == MouseButton.WheelDown)
            {
                Select(Selected + 1);
            }
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
        if (HoldPoint == null)
        {
            return;
        }

        foreach (Node child in HoldPoint.GetChildren())
        {
            child.QueueFree();
        }

        if (SelectedItem != null && SelectedItem.WorldModel != null)
        {
            HoldPoint.AddChild(SelectedItem.WorldModel.Instantiate());
        }
    }
}
