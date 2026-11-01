using Godot;
using Godot.Collections;

public partial class NPCInventory : Storage
{
    // Global acess 
    public static NPCInventory Instance { get; private set; }

    // Holdpoint of the npc if they have something in their hand 
    // TODO: Change in the future so the arm sticks out and they hold it
    [Export] public Marker3D HoldPoint;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        Refresh();
    }
}