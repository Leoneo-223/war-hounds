using Godot;

// For now, copy of Part.cs
public partial class Gun : Node3D
{
    public GunData Data { get; private set; }
    public GunCategory GunType => Data.GunType;
    public void Init(GunData data)
    {
        Data = data;
    }
}