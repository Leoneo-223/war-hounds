
using Godot;

public partial class PartBox : VBoxContainer
{
	[Signal] public delegate void ClickedEventHandler(ItemData data);

	[Export] public TextureRect Icon;
	[Export] public Label NameLabel;
	[Export] public Label DescLabel;

	public ItemData Data { get; private set; }

	private ulong _lastClickFrame;

	public override void _Ready()
	{
		GuiInput += _OnGUIInput;
	}

	public void Setup(ItemData data, int count = 1)
	{
		Data = data;
		Icon.Texture = data.Icon;

		NameLabel.Text = count > 1
			? $"{data.Name} x{count}"
			: data.Name;

		DescLabel.Text = data.Desc;
	}

	private void _OnGUIInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton click &&
			click.Pressed &&
			click.ButtonIndex == MouseButton.Left)
		{
			ulong frame = Engine.GetProcessFrames();

			if (frame == _lastClickFrame)
				return;

			_lastClickFrame = frame;
			EmitSignal(SignalName.Clicked, Data);
		}
	}
}
