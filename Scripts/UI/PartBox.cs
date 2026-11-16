using Godot;

// Attach this to the VBoxContainer root.
public partial class PartBox : VBoxContainer
{
	[Signal] public delegate void ClickedEventHandler(PartData data);

	[Export] public TextureRect Icon;
	[Export] public Label NameLabel;
	[Export] public Label DescLabel;

	public PartData Data { get; private set; }

	public void Setup(PartData data)
	{
		Data = data;
		Icon.Texture = data.Icon;
		NameLabel.Text = data.Name;
		DescLabel.Text = data.Desc;
	}

	// Connected to gui_input in the Node dock.
	private void _OnGUIInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton click && click.Pressed && click.ButtonIndex == MouseButton.Left)
		{
			EmitSignal(SignalName.Clicked, Data);
		}
	}
}