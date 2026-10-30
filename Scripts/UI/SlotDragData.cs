using Godot;

// The "package" that travels with the mouse while dragging an item.
// It only remembers where the item came from; the drop target does the actual move.
public partial class SlotDragData : RefCounted
{
    public Storage From;     // which container the item is being dragged out of
    public int FromSlot;     // slot index inside that container
    public int Amount;       // how many items are being dragged
}