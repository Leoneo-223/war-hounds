using Godot;
using System.Collections.Generic;

// This should not inherit Station as its a different kind, one where the player interacts with
public partial class WorkBench : StaticBody3D, IInteractable
{
    [Export] public Camera3D WorkBenchCamera;
    [Export] public PartListUI WorkBenchUI;
    [Export] public Marker3D PartSpawn;
    [Export] public PackedScene PickupScene;

    // How fast WASD turns a held part (radians per second).
    [Export] public float RotateSpeed = 2.5f;

    private Camera3D _previousCamera;
    private bool _inWorkBench;
    private ItemPickup _heldItem;

    public override void _Ready()
    {
        WorkBenchUI.Visible = false;
        WorkBenchUI.PartSelected += OnPartSelected;
    }

    public virtual string GetPrompt()
    {
        return "[E] to use the workbench";
    }

    public virtual void Interact()
    {
        Enter();
    }

    public void Enter()
    {
        if (_inWorkBench)
        {
            return;
        }

        _previousCamera = GetViewport().GetCamera3D();
        WorkBenchCamera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        WorkBenchUI.Visible = true;
        _inWorkBench = true;
        GameState.UiOpen = true;

        RefreshList();
    }

    public void Exit()
    {
        DropItem();

        _previousCamera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        WorkBenchUI.Visible = false;
        _inWorkBench = false;
        GameState.UiOpen = false;
    }

    // Rebuilds the list from the inventory, one row per part with its count.
    private void RefreshList()
    {
        WorkBenchUI.ClearParts();

        var shown = new List<PartData>();

        foreach (var item in Inventory.Instance.GetItemsByCategory(ItemCategory.Component))
        {
            if (item is PartData part && !shown.Contains(part))
            {
                shown.Add(part);
                WorkBenchUI.AddPart(part, Inventory.Instance.CountOf(part));
            }
        }
    }

    private void OnPartSelected(PartData data)
    {
        // Take one out of the inventory. If there is none left, do nothing.
        if (!Inventory.Instance.RemoveOne(data))
        {
            return;
        }

        var pickup = PickupScene.Instantiate<ItemPickup>();
        pickup.Item = data;
        AddChild(pickup);
        pickup.GlobalPosition = PartSpawn.GlobalPosition;

        RefreshList();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_inWorkBench)
        {
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            Exit();
            GetViewport().SetInputAsHandled();
        }

        // Clicks on the UI never get here, only clicks on the 3D scene do.
        if (@event is InputEventMouseButton click && click.Pressed && click.ButtonIndex == MouseButton.Left)
        {
            TryGrab();
        }
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_heldItem))
        {
            return;
        }

        // Letting go of the mouse button drops the part.
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            DropItem();
            return;
        }

        MoveHeldItem();
        RotateHeldItem((float)delta);
    }

    // Looks for a part under the mouse and picks it up.
    private void TryGrab()
    {
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 from = WorkBenchCamera.ProjectRayOrigin(mouse);
        Vector3 to = from + WorkBenchCamera.ProjectRayNormal(mouse) * 100f;

        // Only look at layer 2, where the pickups live.
        var query = PhysicsRayQueryParameters3D.Create(from, to, 1u << 1);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (result.Count == 0)
        {
            return;
        }

        var pickup = result["collider"].AsGodotObject() as ItemPickup;

        if (pickup == null)
        {
            return;
        }

        _heldItem = pickup;
        _heldItem.LinearVelocity = Vector3.Zero;
        _heldItem.AngularVelocity = Vector3.Zero;
        _heldItem.FreezeMode = RigidBody3D.FreezeModeEnum.Kinematic;
        _heldItem.Freeze = true;   // we move it ourselves while it is held
    }

    // Lets go of the held part so physics takes over again.
    private void DropItem()
    {
        if (IsInstanceValid(_heldItem))
        {
            _heldItem.Freeze = false;
        }

        _heldItem = null;
    }

    // Keeps the part under the mouse, at the height of PartSpawn.
    private void MoveHeldItem()
    {
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 from = WorkBenchCamera.ProjectRayOrigin(mouse);
        Vector3 direction = WorkBenchCamera.ProjectRayNormal(mouse);

        var plane = new Plane(Vector3.Up, PartSpawn.GlobalPosition.Y);
        Vector3? hit = plane.IntersectsRay(from, direction);

        if (hit != null)
        {
            _heldItem.GlobalPosition = hit.Value;
        }
    }

    // A and D turn the part left and right, W and S tilt it.
    private void RotateHeldItem(float delta)
    {
        float amount = RotateSpeed * delta;

        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            _heldItem.GlobalRotate(Vector3.Up, amount);
        }

        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            _heldItem.GlobalRotate(Vector3.Up, -amount);
        }

        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            _heldItem.GlobalRotate(WorkBenchCamera.GlobalBasis.X, -amount);
        }

        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            _heldItem.GlobalRotate(WorkBenchCamera.GlobalBasis.X, amount);
        }
    }
}