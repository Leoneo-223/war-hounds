
using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public class PlayerSaveData
{
    public int Cash { get; set; }
    public int Bank { get; set; }
    public int Level { get; set; } = 1;
    public int XP { get; set; }

    public List<string> UnlockedGunPaths { get; set; } = new();
}

public partial class PlayerData
{
    public int Cash { get; set; } = 0;
    public int Bank { get; set; } = 0;

    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;

    public Array<GunData> UnlockedGuns { get; set; } = new();

    public bool HasGun(GunData gun)
    {
        return gun != null && UnlockedGuns.Contains(gun);
    }

    public bool UnlockGun(GunData gun)
    {
        if (gun == null || HasGun(gun))
            return false;

        UnlockedGuns.Add(gun);
        return true;
    }

    public bool LockGun(GunData gun)
    {
        if (!HasGun(gun))
            return false;

        UnlockedGuns.Remove(gun);
        return true;
    }

    public bool RemoveCash(int amount)
    {
        if (amount <= 0 || Cash < amount)
            return false;

        Cash -= amount;
        return true;
    }

    public void AddCash(int amount)
    {
        Cash += Math.Max(0, amount);
    }

    public void AddBank(int amount)
    {
        Bank += Math.Max(0, amount);
    }
}
