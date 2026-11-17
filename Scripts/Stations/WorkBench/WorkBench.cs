using Godot;
using System;
using System.Collections.Generic;

// This should not inherit Station as its a different kind, one where the player interacts with
public partial class WorkBench : StaticBody3D, IInteractable
{
    [Export] public Camera3D WorkBenchCamera;
    [Export] public PartListUI WorkBenchUI;
    [Export] public Marker3D PartSpawn;
    [Export] public PackedScene PickupScene;

    [Export] public float RotateSpeed = 2.5f;
    [Export] public float FollowSpeed = 20f;
    [Export] public float DepthStep = 0.05f;

    [Export] public Vector3 HoldAreaSize = new Vector3(1.2f, 1f, 0.8f);

    private Camera3D _previousCamera;
    private bool _inWorkBench;
    private ItemPickup _heldItem;
    private Plane _grabPlane;
    private Vector3 _localGrab;
    private GunData _selectedGun;
    private PlayerData _playerData;

    public override void _Ready()
    {
        _playerData = SaveManager.Data;

        WorkBenchUI.Visible = false;
        WorkBenchUI.ItemSelected += OnItemSelected;
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

        ShowGunList();
    }

    public void Exit()
    {
        DropItem();
        ReturnItems();

        _previousCamera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        WorkBenchUI.Visible = false;
        _inWorkBench = false;
        GameState.UiOpen = false;
    }

    private void ShowGunList()
    {
        WorkBenchUI.ClearParts();

        foreach (var gun in _playerData.UnlockedGuns)
        {
            WorkBenchUI.AddPart(gun);
        }
    }

    private void ShowGunParts(GunData gun)
    {
        _selectedGun = gun;

        WorkBenchUI.ClearParts();

        foreach (var part in gun.GunParts)
        {
            int count = Inventory.Instance.CountOf(part);

            if (count > 0)
                WorkBenchUI.AddPart(part, count);
        }
    }

    // Rebuilds the list from the inventory, one row per part with its count.
    // private void RefreshList()
    // {
    //     WorkBenchUI.ClearParts();
    // 
    //     var shown = new List<PartData>();
    // 
    //     foreach (var item in Inventory.Instance.GetItemsByCategory(ItemCategory.Component))
    //     {
    //         if (item is PartData part && !shown.Contains(part))
    //         {
    //             shown.Add(part);
    //             WorkBenchUI.AddPart(part, Inventory.Instance.CountOf(part));
    //         }
    //     }
    // }

    private void OnItemSelected(ItemData data)
    {
        // Player selected a gun
        if (data is GunData gun)
        {
            ShowGunParts(gun);
            return;
        }

        // Player selected a part
        if (data is PartData part)
        {
            if (_selectedGun == null)
                return;

            if (!Inventory.Instance.RemoveOne(part))
                return;

            var pickup = PickupScene.Instantiate<ItemPickup>();

            pickup.Item = part;

            AddChild(pickup);
            pickup.GlobalPosition = PartSpawn.GlobalPosition;
        }
    }

    // Runs before the UI sees the input, so a HUD node that blocks clicks can't hide them.
    public override void _Input(InputEvent @event)
    {
        if (!_inWorkBench)
        {
            return;
        }

        if (@event is InputEventMouseButton click && click.Pressed)
        {
            if (click.ButtonIndex == MouseButton.Left)
            {
                // A click on the parts panel belongs to the UI, so leave it alone.
                var hovered = GetViewport().GuiGetHoveredControl();

                if (hovered != null && WorkBenchUI.IsAncestorOf(hovered))
                {
                    return;
                }

                TryGrab();
            }

            // Mouse wheel pushes the held part away or pulls it closer.
            if (click.ButtonIndex == MouseButton.WheelUp)
            {
                PushHeldItem(DepthStep);
            }

            if (click.ButtonIndex == MouseButton.WheelDown)
            {
                PushHeldItem(-DepthStep);
            }
        }
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
    }

    public override void _PhysicsProcess(double delta)
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
        RotateHeldItem();
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

        // Remember the exact spot we clicked, so it stays under the mouse while dragging.
        Vector3 hitPoint = result["position"].AsVector3();
        _localGrab = pickup.GlobalBasis.Inverse() * (pickup.GlobalPosition - hitPoint);

        // An upright plane facing the camera: mouse up/down raises and lowers, left/right moves sideways.
        Vector3 forward = -WorkBenchCamera.GlobalBasis.Z;
        forward.Y = 0f;
        _grabPlane = new Plane(forward.Normalized(), hitPoint);

        // The part stays a normal physics body, so it can't go through walls or other parts.
        _heldItem.Sleeping = false;
        _heldItem.ContinuousCd = true;
        _heldItem.GravityScale = 0f;
        _heldItem.LinearVelocity = Vector3.Zero;
        _heldItem.AngularVelocity = Vector3.Zero;
    }

    // Lets go of the held part so gravity takes over again.
    private void DropItem()
    {
        if (IsInstanceValid(_heldItem))
        {
            _heldItem.GravityScale = 1f;
            _heldItem.LinearVelocity = Vector3.Zero;
            _heldItem.AngularVelocity = Vector3.Zero;
        }

        _heldItem = null;
    }

    // Flies the part towards the mouse. Physics stops it at walls and other parts.
    private void MoveHeldItem()
    {
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 from = WorkBenchCamera.ProjectRayOrigin(mouse);
        Vector3 direction = WorkBenchCamera.ProjectRayNormal(mouse);

        Vector3? hit = _grabPlane.IntersectsRay(from, direction);

        if (hit == null)
        {
            return;
        }

        Vector3 target = hit.Value + _heldItem.GlobalBasis * _localGrab;

        Vector3 local = PartSpawn.ToLocal(target);
        local.X = Mathf.Clamp(local.X, -HoldAreaSize.X / 2f, HoldAreaSize.X / 2f);
        local.Y = Mathf.Clamp(local.Y, -HoldAreaSize.Y / 2f, HoldAreaSize.Y / 2f);
        local.Z = Mathf.Clamp(local.Z, -HoldAreaSize.Z / 2f, HoldAreaSize.Z / 2f);
        target = PartSpawn.ToGlobal(local);

        Vector3 velocity = (target - _heldItem.GlobalPosition) * FollowSpeed;
        _heldItem.LinearVelocity = velocity.LimitLength(10f);
    }

    // Puts every part that is on the bench back into the inventory.
    private void ReturnItems()
    {
        foreach (Node child in GetChildren())
        {
            if (child is ItemPickup pickup && pickup.Item != null)
            {
                // Add() returns how many did NOT fit.
                int leftover = Inventory.Instance.Add(pickup.Item, pickup.Count);

                if (leftover <= 0)
                {
                    pickup.QueueFree();
                }
                else
                {
                    pickup.Count = leftover;
                }
            }
        }
    }

    private void RotateHeldItem()
    {
        Vector3 spin = Vector3.Zero;

        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            spin += Vector3.Up;
        }

        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            spin -= Vector3.Up;
        }

        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            spin -= WorkBenchCamera.GlobalBasis.X;
        }

        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            spin += WorkBenchCamera.GlobalBasis.X;
        }

        if (Input.IsPhysicalKeyPressed(Key.Q))
        {
            spin -= WorkBenchCamera.GlobalBasis.Z;
        }

        if (Input.IsPhysicalKeyPressed(Key.E))
        {
            spin += WorkBenchCamera.GlobalBasis.Z;
        }

        _heldItem.AngularVelocity = spin * RotateSpeed;
    }

    // Moves the plane the part slides on, which pushes the part away or pulls it closer.
    private void PushHeldItem(float amount)
    {
        if (!IsInstanceValid(_heldItem))
        {
            return;
        }

        _grabPlane = new Plane(_grabPlane.Normal, _grabPlane.D + amount);
    }
}