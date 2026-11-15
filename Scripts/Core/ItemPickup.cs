using Godot;

// One generic scene is reused for every item, you only change the Item field.
public partial class ItemPickup : RigidBody3D, IInteractable
{
    // Which item this is.
    [Export] public ItemData Item;

    // How many items are in this pile.
    [Export] public int Count = 1;

    public override void _Ready()
    {
        if (Item != null && Item.WorldModel != null)
        {
            Node3D model = Item.WorldModel.Instantiate<Node3D>();

            AddChild(model);

            foreach (Node child in model.FindChildren("*", "MeshInstance3D", true, false))
            {
                MeshInstance3D meshInstance = (MeshInstance3D)child;

                if (meshInstance.Mesh == null)
                    continue;

                CollisionShape3D collision = new CollisionShape3D();

                collision.Shape = meshInstance.Mesh.CreateConvexShape();

                AddChild(collision);

                collision.GlobalTransform = meshInstance.GlobalTransform;
            }
        }

        CollisionLayer = 1u << 1;
        CollisionMask = 1u | (1u << 1);
    }

    // Shown by the HUD when the player looks at this.
    public string GetPrompt()
    {
        if (Item == null)
        {
            return "";
        }

        if (Count > 1)
        {
            return $"[E] Pick up {Item.Name} x{Count}";
        }
        else
        {
            return $"[E] Pick up {Item.Name}";
        }
    }

    public void Interact()
    {
        if (Item == null)
        {
            return;
        }

        // Add() returns how many items did NOT fit.
        int leftover = Inventory.Instance.Add(Item, Count);

        if (leftover <= 0)
        {
            QueueFree();        // everything fit, remove from the world
        }
        else
        {
            Count = leftover;   // inventory full, leave the remainder here
        }
    }
}
