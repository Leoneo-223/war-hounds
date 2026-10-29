using Godot;

// Attach to the root of your HUD scene (PlayerHud).
// Shows the interaction prompt and makes the crosshair react when hovering something.
// If your HUD root is a CanvasLayer instead of a Control, change ": Control" to ": CanvasLayer".
public partial class Hud : CanvasLayer
{
    // Label that shows "[E] Pick up Metal Pipe". Drag it in via the Inspector.
    [Export] public Label Prompt;

    // Your crosshair node (the TextureRect with the PNG). Drag it in.
    [Export] public Control Crosshair;

    // How much the crosshair grows while you look at something interactable.
    [Export] public float HoverScale = 1.4f;

    private bool _hovering;

    public override void _Ready() => SetPrompt("");

    // Called by the Interactor every frame with the current prompt text.
    public void SetPrompt(string text)
    {
        _hovering = text != "";

        if (Prompt != null)
        {
            Prompt.Text = text;
            Prompt.Visible = _hovering;
        }
    }

    public override void _Process(double delta)
    {
        if (Crosshair == null) return;

        // Scale from the middle of the image, not its top-left corner.
        Crosshair.PivotOffset = Crosshair.Size / 2f;

        // Smoothly ease toward the target scale.
        Vector2 target = Vector2.One * (_hovering ? HoverScale : 1f);
        Crosshair.Scale = Crosshair.Scale.Lerp(target, 1f - Mathf.Exp(-15f * (float)delta));
    }
}