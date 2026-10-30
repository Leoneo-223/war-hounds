using Godot;

// Attach to a RayCast3D that is a child of the player's Camera3D.
// Each physics frame it checks what the player is looking at and shows its prompt.
// Pressing E triggers Interact() on it.
public partial class Interactor : RayCast3D
{
    // The HUD node (the one with Hud.cs). Drag it in via the Inspector.
    [Export] public Hud Hud;

    private IInteractable _current;

    public override void _Ready()
    {
        CollisionMask |= 1u << 1;   // keep layer 1 and add layer 2
        // The ray starts inside the player's capsule, so make sure it can never
        // hit the player itself. Walk up the tree to find the player body.
        Node n = GetParent();
        while (n != null)
        {
            if (n is CollisionObject3D body) { AddException(body); break; }
            n = n.GetParent();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // GetCollider() returns whatever the ray hit (or null).
        // "as IInteractable" gives null for walls, floors and anything not interactable.
        _current = IsColliding() ? GetCollider() as IInteractable : null;

        Hud?.SetPrompt(_current?.GetPrompt() ?? "");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // THE EXACT SAME, as PlayerController.cs
        if (GameState.UiOpen) return;

        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.E })
            _current?.Interact();
    }
}