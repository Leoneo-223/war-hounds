using Godot;

public partial class Interactor : RayCast3D
{
    [Export] public Hud Hud;

    private IInteractable _current;

    public override void _Ready()
    {
        CollisionMask |= 1u << 1;

        // Make sure the player never gets hit by the raycast
        Node n = GetParent();
        while (n != null)
        {
            CollisionObject3D body = n as CollisionObject3D;
            if (body != null)
            {
                AddException(body);
                break;
            }
            n = n.GetParent();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // GetCollider() returns whatever the ray hit (or null).
        // "as IInteractable" gives null for walls, floors and anything not interactable.
        _current = null;
        if (IsColliding())
        {
            _current = GetCollider() as IInteractable;
        }

        string prompt = "";
        if (_current != null)
        {
            prompt = _current.GetPrompt();
            if (prompt == null)
            {
                prompt = "";
            }
        }

        if (Hud != null)
        {
            Hud.SetPrompt(prompt);
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Same as in Player.cs if a UI is open, do nothing
        if (GameState.UiOpen)
        {
            return;
        }

        InputEventKey keyEvent = e as InputEventKey;
        if (keyEvent != null && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.PhysicalKeycode == Key.E)
            {
                if (_current != null)
                {
                    _current.Interact();
                }
            }

            if (keyEvent.PhysicalKeycode == Key.F)
            {
                ISecondaryInteractable secondary = _current as ISecondaryInteractable;
                if (secondary != null)
                {
                    secondary.InteractSecondary();
                }
            }
        }
    }
}
