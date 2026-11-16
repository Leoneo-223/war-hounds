using Godot;
using System;

public partial class PartBox : VBoxContainer
{
	[Export] public TextureRect Icon;
	[Export] public Label NameLabel;
	[Export] public Label DescLabel;

	public PartData Data { get; private set; } // Anyone can get the data, only this class can set it

	public void Setup(PartData data)
	{
		Data = data;
		Icon.Texture = data.Icon;
		NameLabel.Text = data.Name;
		DescLabel.Text = data.Desc;
	}
}

