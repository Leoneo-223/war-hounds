using Godot;

// A world object that stores items. Root is a StaticBody3D with a CollisionShape3D,
// a mesh, and a child Storage node (drag it into the Contents slot).
// Pressing E opens the drag-and-drop window. Works for crates, barrels, racks...
// just change CrateName, the mesh and the Storage settings per scene.
public partial class StorageCrate : StaticBody3D, IInteractable
{
    [Export] public string CrateName = "Crate";

    // The Storage child node that holds the items.
    [Export] public Storage Contents;

    public string GetPrompt()
    {
        if (Contents == null)
        {
            return "";
        }

        return $"[E] Open {CrateName}";
    }

    public void Interact()
    {
        if (Contents == null)
        {
            return;
        }

        if (StorageUI.Instance != null)
        {
            StorageUI.Instance.Open(Contents, CrateName);
        }
    }
}
