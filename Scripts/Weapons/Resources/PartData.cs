using Godot;

public enum PartCategory
{
    Body,
    Barrel,
    Handle,
    Magazine,
    Stock,
    Sight,
    Slide,
}

[GlobalClass]
public partial class PartData : ItemData
{
    [Export] public PartCategory PartType;

    public PartData()
    {
        Category = ItemCategory.Component; // every part is a Component item
        MaxStack = 1;
    }
}