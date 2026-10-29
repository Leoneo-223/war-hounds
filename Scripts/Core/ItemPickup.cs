using Godot;

// Root script of Pickup.tscn (RigidBody3D + CollisionShape3D).
// One generic scene is reused for every item: you only change the Item field.
public partial class ItemPickup : RigidBody3D, IInteractable
{
    // Which item this is. Set in the Inspector, or by code when dropped.
    [Export] public ItemData Item;

    // How many items are in this pile.
    [Export] public int Count = 1;

    public override void _Ready()
    {
        // The pickup scene has no visuals of its own.
        // We spawn the item's model as a child so the same scene works for all items.
        if (Item?.WorldModel != null)
            AddChild(Item.WorldModel.Instantiate());
            
        // Pickups live on layer 2 (bit value 2), so the player (which only collides with layer 1) walks through them.
        CollisionLayer = 1u << 1;
        // They still collide with layer 1 (the floor and walls), so they land on surfaces,
        // but not with each other or with other pickups.
        CollisionMask = 1u;
    }

    // Shown by the HUD when the player looks at this.
    public string GetPrompt()
    {
        if (Item == null) return "";
        return Count > 1
            ? $"[E] Pick up {Item.Name} x{Count}"
            : $"[E] Pick up {Item.Name}";
    }

    public void Interact()
    {
        if (Item == null) return;

        // Add() returns how many items did NOT fit.
        int leftover = Inventory.Instance.Add(Item, Count);

        if (leftover <= 0)
            QueueFree();        // everything fit, remove from the world
        else
            Count = leftover;   // inventory full, leave the remainder here
    }
}