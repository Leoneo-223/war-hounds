using Godot;

// Attach this to the VBoxContainer root.
public partial class PartBox : VBoxContainer
{
	[Signal] public delegate void ClickedEventHandler(PartData data);

	[Export] public TextureRect Icon;
	[Export] public Label NameLabel;
	[Export] public Label DescLabel;

	public PartData Data { get; private set; }

	private ulong _lastClickFrame;

	public override void _Ready()
	{
		GuiInput += _OnGUIInput;
	}

	public void Setup(PartData data, int count)
	{
		Data = data;
		Icon.Texture = data.Icon;
		NameLabel.Text = $"{data.Name} x{count}";
		DescLabel.Text = data.Desc;
	}

	// Runs when the mouse does something on this row.
	private void _OnGUIInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton click && click.Pressed && click.ButtonIndex == MouseButton.Left)
		{
			// One click only counts once, even if gui_input is connected twice.
			ulong frame = Engine.GetProcessFrames();

			if (frame == _lastClickFrame)
			{
				return;
			}

			_lastClickFrame = frame;
			EmitSignal(SignalName.Clicked, Data);
		}
	}
}