
using Godot;

public partial class PartListUI : Control
{
	[Signal]
	public delegate void ItemSelectedEventHandler(ItemData data);

	[Export] public PackedScene PartBoxScene;
	[Export] public VBoxContainer PartList;

	public override void _Ready()
	{
	}

	public void AddPart(ItemData data, int count = 1)
	{
		var box = PartBoxScene.Instantiate<PartBox>();

		PartList.AddChild(box);
		box.Setup(data, count);

		box.Clicked += OnItemClicked;
	}

	public void ClearParts()
	{
		foreach (Node child in PartList.GetChildren())
			child.QueueFree();
	}

	private void OnItemClicked(ItemData data)
	{
		EmitSignal(SignalName.ItemSelected, data);
	}
}
