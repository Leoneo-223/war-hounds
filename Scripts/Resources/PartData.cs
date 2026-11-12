using Godot;

public enum PartCategory
{
    GunBase,
    Slide,
    Barrel,
    Magazine,
    Stock,
    Muzzle,
    Grip,
    Lazer,
    Flashlight,
    Bayonet
}

[GlobalClass]
public partial class PartData : ItemData
{
    [Export] public PartCategory PartType;
    [Export] public PackedScene Mesh;
    [Export] public float Weight;
}