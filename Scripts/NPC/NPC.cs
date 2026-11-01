using Godot;
using Godot.Collections;

public partial class NPC : CharacterBody3D, IInteractable
{
    [ExportGroup("DATA")]
    [Export] public NPCData Data;

    [ExportGroup("MOVEMENT")]
    [Export] public float WalkSpeed = 1.6f;
    [Export] public float TurnSpeed = 6f;

    [ExportGroup("NODES")]
    [Export] public NavigationAgent3D Agent;
    [Export] public Godot.Collections.Array<Marker3D> Waypoints = new Godot.Collections.Array<Marker3D>();


    // IInteractable functions, getprompt and interact
    public virtual string GetPrompt()
    {
        return "";
    }

    public virtual void Interact()
    {

    }
}