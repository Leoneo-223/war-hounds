using System;
using Godot;
using Godot.Collections;

public enum GunCategory
{
    Pistol,
    Rifle,
    Shotgun,
}

[GlobalClass]
public partial class GunData : ItemData
{
    [Export] public GunCategory GunType;
    [Export] public bool Unlocked;
    [Export] public Array<PartData> GunParts;

    public GunData()
    {
        Category = ItemCategory.Weapon;
        MaxStack = 1;
    }
}