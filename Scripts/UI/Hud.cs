using Godot;

public partial class Hud : CanvasLayer
{
    [Export] public Label Prompt;
    [Export] public bool UseItemName = false;
    [Export] public Control Crosshair;
    [Export] public float HoverScale = 1.4f;

    private bool _hovering;

    public override void _Ready()
    {
        SetPrompt("");
    }

    // Called by the Interactor every frame with the current prompt text.
    public void SetPrompt(string text)
    {
        // Hide all prompts while a menu is open.
        if (GameState.UiOpen)
        {
            text = "";
        }

        _hovering = text != "";

        if (Prompt != null)
        {
            if (UseItemName)
            {
                Prompt.Text = text;
            }
            Prompt.Visible = _hovering;
        }
    }

    public override void _Process(double delta)
    {
        if (Crosshair == null)
        {
            return;
        }

        // Scale from the middle of the image, not its top-left corner.
        Crosshair.PivotOffset = Crosshair.Size / 2f;

        // Smoothly ease toward the target scale.
        float targetSize = 1f;
        if (_hovering)
        {
            targetSize = HoverScale;
        }

        Vector2 target = Vector2.One * targetSize;
        Crosshair.Scale = Crosshair.Scale.Lerp(target, 1f - Mathf.Exp(-15f * (float)delta));
    }
}
