using Godot;
using Godot.Collections;

// The parts panel on the workstation. Put this on the "Control" node.
public partial class PartListUI : Control
{
	[Signal] public delegate void PartSelectedEventHandler(PartData data);

	[Export] public PackedScene PartBoxScene;
	[Export] public VBoxContainer PartList;

	[Export] public Array<PartData> Parts = new();

	public override void _Ready()
	{
		foreach (var part in Parts)
		{
			AddPart(part);
		}
	}

	public void AddPart(PartData data, int count = 1)
	{
		var box = PartBoxScene.Instantiate<PartBox>();
		PartList.AddChild(box);
		box.Setup(data, count);
		box.Clicked += OnPartClicked;
	}

	public void ClearParts()
	{
		foreach (Node child in PartList.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void OnPartClicked(PartData data)
	{
		EmitSignal(SignalName.PartSelected, data);
	}
}