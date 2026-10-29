using Godot;
using System.Linq;

// Attach to VBoxContainer (or any node above the slots).
// Mirrors the Inventory into your Slot1..Slot10 nodes and shows
// the selected item's name in a label.
public partial class Hotbar : Node
{
    // Color of the selected slot's background.
    [Export] public Color SelectedColor = new Color(1f, 0.85f, 0.3f);

    // Label that shows the held item's name. Drag it in via the Inspector.
    [Export] public Label SelectedItemLabel;

    private Node[] _slots;

    public override void _Ready()
    {
        // Find every node named Slot* anywhere below this one.
        // Tree order is preserved, so Slot1 is index 0, and so on.
        _slots = FindChildren("Slot*", "", true, false).ToArray();

        // The Inventory may not exist yet (the Player might load after the HUD),
        // so wait one frame before connecting to it.
        CallDeferred(nameof(Hook));
    }

    private void Hook()
    {
        Inventory.Instance.Changed += Refresh;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.Changed -= Refresh;
    }

    private void Refresh()
    {
        var inv = Inventory.Instance;

        for (int i = 0; i < _slots.Length && i < inv.SlotCount; i++)
        {
            ItemData item = inv.Items[i];

            // Show the item's icon, or hide the icon when the slot is empty.
            var icon = _slots[i].GetNode<TextureRect>("Icon");
            icon.Texture = item?.Icon;
            icon.Visible = item != null;

            // Stack number: only shown when there's more than one item.
            var count = _slots[i].GetNodeOrNull<Label>("Count");
            if (count != null)
                count.Text = inv.Counts[i] > 1 ? inv.Counts[i].ToString() : "";

            // Highlight the selected slot by tinting its background.
            _slots[i].GetNode<CanvasItem>("Background").SelfModulate =
                i == inv.Selected ? SelectedColor : Colors.White;
        }

        UpdateSelectedLabel(inv);
    }

    // Shows "Planks" or "Planks x5" for the held item, and hides the label when the slot is empty.
    private void UpdateSelectedLabel(Inventory inv)
    {
        if (SelectedItemLabel == null) return;

        ItemData held = inv.SelectedItem;
        if (held == null)
        {
            SelectedItemLabel.Visible = false;
            return;
        }

        int count = inv.Counts[inv.Selected];
        SelectedItemLabel.Text = count > 1 ? $"{held.Name} x{count}" : held.Name;
        SelectedItemLabel.Visible = true;
    }
}