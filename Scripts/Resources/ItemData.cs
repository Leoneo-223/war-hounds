using Godot;
using System;

public enum ItemCategory
{
    Consumable,
    Weapon,
    Ammo,
    Attachment,
    Tool,
    Material,
    Component,
    KeyItem,
    Miscellaneous,
    Clothing
}

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id;
    [Export] public string Name;
    [Export] public Texture2D Icon;
    [Export] public PackedScene WorldModel;
    [Export] public int MaxStack = 1;
    [Export] public ItemCategory Category;
}
