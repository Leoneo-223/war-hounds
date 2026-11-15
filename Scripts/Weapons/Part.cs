using Godot;

public partial class Part : Node3D
{
    // Which kind of part this is (set by Init, not in the Inspector, to avoid a cyclic reference
    public PartData Data { get; private set; }

    // Shortcut: read the part's category straight from its PartData
    public PartCategory PartType => Data.PartType;

    // Call right after instantiating the part, before AddChild
    public void Init(PartData data)
    {
        Data = data;
    }
}