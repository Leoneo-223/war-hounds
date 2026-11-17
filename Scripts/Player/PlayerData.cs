
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

public partial class PlayerData : GodotObject
{
    [Signal]
    public delegate void CashChangedEventHandler(int newAmount);

    [Signal]
    public delegate void BankChangedEventHandler(int newAmount);

    private int _cash = 0;
    private int _bank = 0;

    public int Cash
    {
        get => _cash;
        set
        {
            if (_cash == value) return;
            _cash = value;
            EmitSignal(SignalName.CashChanged, _cash);
        }
    }

    public int Bank
    {
        get => _bank;
        set
        {
            if (_bank == value) return;
            _bank = value;
            EmitSignal(SignalName.BankChanged, _bank);
        }
    }

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
        {
            return false;
        }

        UnlockedGuns.Add(gun);
        return true;
    }

    public bool LockGun(GunData gun)
    {
        if (!HasGun(gun))
        {
            return false;
        }

        UnlockedGuns.Remove(gun);
        return true;
    }

    public void AddCash(int amount)
    {
        Cash += Math.Max(0, amount);
    }

    public bool RemCash(int amount)
    {
        if (amount <= 0 || Cash < amount)
        {
            return false;
        }

        Cash -= amount;
        return true;
    }

    public void AddBank(int amount)
    {
        Bank += Math.Max(0, amount);
    }

    public bool RemBank(int amount)
    {
        if (amount <= 0 || Bank < amount)
        {
            return false;
        }

        Bank -= amount;
        return true;
    }
}
