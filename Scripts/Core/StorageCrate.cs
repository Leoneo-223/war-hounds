using Godot;

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
