using Godot;
using System;
using Godot.Collections;

public partial class PartListUI : Control
{
	[Export] public PackedScene PartBoxScene;
	[Export] public VBoxContainer PartList;

	// For Testing
	[Export] public Array<PartData> Parts = new();

	public override void _Ready()
	{
		foreach (var part in Parts)
		{
			AddPart(part);
		}
	}

	public void AddPart(PartData data)
	{
		var box = PartBoxScene.Instantiate<PartBox>();
		PartList.AddChild(box);
		box.Setup(data);
	}

	public void ClearParts()
	{
		foreach (Node child in PartList.GetChildren())
		{
			child.QueueFree();
		}
	}
}
