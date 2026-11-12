using Godot;
using Godot.Collections;

public enum WeaponCategory
{
    Pistol,
    AssaultRifle,
    Shotgun,
    SMG,
    LMG
}

[GlobalClass]
public partial class WeaponData : ItemData
{
    [Export] public WeaponCategory WeaponType;

    // Stats
    [Export] public float Damage;
    [Export] public float Accuracy;
    [Export] public float Weight;
    [Export] public float MaxDurability;

    // Runtime durability
    [Export] public float Durability;

    // Installed parts
    [Export] public Array<ItemData> Parts = new();
}