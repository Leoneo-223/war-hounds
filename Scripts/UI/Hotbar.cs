using Godot;

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
        Godot.Collections.Array<Node> found = FindChildren("Slot*", "", true, false);
        _slots = new Node[found.Count];
        for (int i = 0; i < found.Count; i++)
        {
            _slots[i] = found[i];
        }

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
        {
            Inventory.Instance.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        Inventory inv = Inventory.Instance;

        for (int i = 0; i < _slots.Length && i < inv.SlotCount; i++)
        {
            ItemData item = inv.Items[i];

            // Show the item's icon, or hide the icon when the slot is empty.
            TextureRect icon = _slots[i].GetNode<TextureRect>("Icon");
            if (item != null)
            {
                icon.Texture = item.Icon;
                icon.Visible = true;
            }
            else
            {
                icon.Texture = null;
                icon.Visible = false;
            }

            // Stack number: only shown when there's more than one item.
            Label count = _slots[i].GetNodeOrNull<Label>("Count");
            if (count != null)
            {
                if (inv.Counts[i] > 1)
                {
                    count.Text = inv.Counts[i].ToString();
                }
                else
                {
                    count.Text = "";
                }
            }

            // Highlight the selected slot by tinting its background.
            CanvasItem background = _slots[i].GetNode<CanvasItem>("Background");
            if (i == inv.Selected)
            {
                background.SelfModulate = SelectedColor;
            }
            else
            {
                background.SelfModulate = Colors.White;
            }
        }

        UpdateSelectedLabel(inv);
    }

    // Shows "Planks" or "Planks x5" for the held item, and hides the label when the slot is empty.
    private void UpdateSelectedLabel(Inventory inv)
    {
        if (SelectedItemLabel == null)
        {
            return;
        }

        ItemData held = inv.SelectedItem;
        if (held == null)
        {
            SelectedItemLabel.Visible = false;
            return;
        }

        int count = inv.Counts[inv.Selected];
        if (count > 1)
        {
            SelectedItemLabel.Text = $"{held.Name} x{count}";
        }
        else
        {
            SelectedItemLabel.Text = held.Name;
        }
        SelectedItemLabel.Visible = true;
    }
}
