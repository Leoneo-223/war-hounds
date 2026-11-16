using Godot;

// This should not inherit Station as its a different kind, one where the player interacts with
public partial class WorkBench : StaticBody3D, IInteractable
{
    [Export] public Camera3D WorkBenchCamera;
    [Export] public PartListUI WorkBenchUI;
    [Export] public Marker3D PartSpawn;
    [Export] public PackedScene PickupScene;

    private Camera3D _previousCamera;
    private bool _inWorkBench;

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

        WorkBenchUI.ClearParts();

        foreach (var item in Inventory.Instance.GetItemsByCategory(ItemCategory.Component))
        {
            if (item is PartData part)
                WorkBenchUI.AddPart(part);
        }
    }

    public void Exit()
    {
        _previousCamera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        WorkBenchUI.Visible = false;
        _inWorkBench = false;
        GameState.UiOpen = false;
    }

    private void OnPartSelected(PartData data)
    {
        GD.Print("Selected part");
        var pickup = PickupScene.Instantiate<ItemPickup>();
        pickup.Item = data;
        AddChild(pickup);
        pickup.GlobalPosition = PartSpawn.GlobalPosition;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_inWorkBench && @event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            Exit();
            GetViewport().SetInputAsHandled();
        }
    }
}