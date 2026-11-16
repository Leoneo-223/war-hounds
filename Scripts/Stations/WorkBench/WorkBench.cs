using Godot;

// This should not inherit Station as its a different kind, one where the player interacts with
public partial class WorkBench : StaticBody3D, IInteractable
{
    [Export] public Camera3D WorkBenchCamera;
    [Export] public PartListUI WorkBenchUI;

    private Camera3D _previousCamera;
    private bool _inWorkBench;

    public override void _Ready()
    {
        WorkBenchUI.Visible = false;
    }

    public virtual string GetPrompt()
    {
        return "[E] to use the workbench";
    }

    public virtual void Interact()
    {
        // If theres any reason why the player can't acess, put it here
        // None for now

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
            {
                WorkBenchUI.AddPart(part);
            }
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

    // if the player is in and ESC is pressed, exit the bench
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_inWorkBench && @event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
        {
            Exit();
            GetViewport().SetInputAsHandled();
        }
    }
}