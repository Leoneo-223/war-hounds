using Godot;

// Attach to the root of your HUD scene (PlayerHud).
// Shows the interaction prompt and makes the crosshair react when hovering something.
public partial class Hud : CanvasLayer
{
    // Label that shows "[E] Pick up Metal Pipe". Drag it in via the Inspector.
    [Export] public Label Prompt;

    // If off, the Prompt label keeps the text you typed in the editor
    // and is only shown/hidden. If on, the text comes from the item.
    [Export] public bool UseItemName = false;

    // Your crosshair node (the TextureRect with the PNG). Drag it in.
    [Export] public Control Crosshair;

    // How much the crosshair grows while you look at something interactable.
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
