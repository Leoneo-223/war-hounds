using Godot;

// Reusable item storage. Add this as a child node to anything that should hold items:
// crates, barrels, racks, the fabricator's input box. The player's Inventory extends it.
public partial class Storage : Node
{
    // Fired whenever contents change. UI listens to this.
    [Signal] public delegate void ChangedEventHandler();

    [Export] public int SlotCount = 10;

    // Optional filter: when on, only items of AllowedCategory can be stored here.
    [Export] public bool RestrictCategory = false;
    [Export] public ItemCategory AllowedCategory = ItemCategory.Miscellaneous;

    // Parallel arrays: slot i holds Items[i] with Counts[i] of them. Null item = empty slot.
    public ItemData[] Items;
    public int[] Counts;

    public override void _Ready()
    {
        Items = new ItemData[SlotCount];
        Counts = new int[SlotCount];
    }

    public bool CanAccept(ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        if (RestrictCategory && item.Category != AllowedCategory)
        {
            return false;
        }

        return true;
    }

    // Tries to add items. Returns how many did NOT fit (0 = all added).
    public int Add(ItemData item, int count)
    {
        if (!CanAccept(item))
        {
            return count;
        }

        // Pass 1: top up existing stacks of the same item.
        for (int i = 0; i < SlotCount && count > 0; i++)
        {
            if (Items[i] != item)
            {
                continue;
            }

            int moved = Mathf.Min(item.MaxStack - Counts[i], count);
            Counts[i] += moved;
            count -= moved;
        }

        // Pass 2: fill empty slots with new stacks.
        for (int i = 0; i < SlotCount && count > 0; i++)
        {
            if (Items[i] != null)
            {
                continue;
            }

            int moved = Mathf.Min(item.MaxStack, count);
            Items[i] = item;
            Counts[i] = moved;
            count -= moved;
        }

        Refresh();
        return count;
    }

    // Removes items from one slot and clears it when it reaches zero.
    public void RemoveFromSlot(int slot, int count = 1)
    {
        if (Items[slot] == null)
        {
            return;
        }

        Counts[slot] -= count;
        if (Counts[slot] <= 0)
        {
            Items[slot] = null;
            Counts[slot] = 0;
        }

        Refresh();
    }

    // Total amount of one item across all slots. Handy for recipe checks.
    public int CountOf(ItemData item)
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

    // Removes an amount of one item from as many slots as needed.
    // Returns false (and removes nothing) if there isn't enough.
    public bool Remove(ItemData item, int count)
    {
        if (CountOf(item) < count)
        {
            return false;
        }

        for (int i = 0; i < SlotCount && count > 0; i++)
        {
            if (Items[i] != item)
            {
                continue;
            }

            int taken = Mathf.Min(Counts[i], count);
            RemoveFromSlot(i, taken);
            count -= taken;
        }
        return true;
    }

    // Index of the first non-empty slot, or -1 if the storage is empty.
    public int FirstFilledSlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Items[i] != null)
            {
                return i;
            }
        }
        return -1;
    }

    // Moves items between two slots (they can be in different storages or the same one).
    //  - empty target slot > items move there
    //  - same item in target > stacks up to MaxStack, the rest stays behind
    //  - different item in target > the two slots swap (whole stacks only)
    // With dryRun = true nothing changes; it only reports whether the move would work.
    // The UI uses that to decide if a slot accepts the item you are dragging.
    public static bool Transfer(Storage src, int srcSlot, Storage dst, int dstSlot, int amount, bool dryRun = false)
    {
        if (src == null || dst == null)
        {
            return false;
        }

        if (src == dst && srcSlot == dstSlot)
        {
            return false;
        }

        ItemData item = src.Items[srcSlot];
        if (item == null)
        {
            return false;
        }

        if (!dst.CanAccept(item))
        {
            return false;
        }

        amount = Mathf.Clamp(amount, 1, src.Counts[srcSlot]);
        ItemData target = dst.Items[dstSlot];

        if (target == null || target == item)
        {
            // Move into an empty slot or stack onto the same item.
            int alreadyThere = 0;
            if (target != null)
            {
                alreadyThere = dst.Counts[dstSlot];
            }

            int space = item.MaxStack - alreadyThere;
            int moved = Mathf.Min(space, amount);
            if (moved <= 0)
            {
                return false;
            }

            if (dryRun)
            {
                return true;
            }

            dst.Items[dstSlot] = item;
            dst.Counts[dstSlot] += moved;

            src.Counts[srcSlot] -= moved;
            if (src.Counts[srcSlot] <= 0)
            {
                src.Items[srcSlot] = null;
                src.Counts[srcSlot] = 0;
            }
        }
        else
        {
            // Different item: swap, but only if the whole stack is being moved
            // and the source container is allowed to hold the other item.
            if (amount != src.Counts[srcSlot])
            {
                return false;
            }

            if (!src.CanAccept(target))
            {
                return false;
            }

            if (dryRun)
            {
                return true;
            }

            int dstCount = dst.Counts[dstSlot];
            dst.Items[dstSlot] = item;
            dst.Counts[dstSlot] = src.Counts[srcSlot];
            src.Items[srcSlot] = target;
            src.Counts[srcSlot] = dstCount;
        }

        src.Refresh();
        if (dst != src)
        {
            dst.Refresh();
        }
        return true;
    }

    // Called after every change. Subclasses (like Inventory) can extend it.
    protected virtual void Refresh()
    {
        EmitSignal(SignalName.Changed);
    }
}
