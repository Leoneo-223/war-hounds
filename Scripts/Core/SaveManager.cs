
using Godot;
using System;
using System.Text.Json;

public partial class SaveManager : Node
{
    public static PlayerData Data { get; private set; } = new();

    private const string SavePath = "user://player_data.json";

    public override void _Ready()
    {
        LoadGame();
    }

    public static void SaveGame()
    {
        var saveData = new PlayerSaveData
        {
            Cash = Data.Cash,
            Bank = Data.Bank,
            Level = Data.Level,
            XP = Data.XP
        };

        foreach (var gun in Data.UnlockedGuns)
        {
            if (gun != null)
                saveData.UnlockedGunPaths.Add(gun.ResourcePath);
        }

        string json = JsonSerializer.Serialize(saveData);

        using var file = FileAccess.Open(
            SavePath,
            FileAccess.ModeFlags.Write
        );

        file?.StoreString(json);
    }

    public static void LoadGame()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            Data = new PlayerData();
            SaveGame();
            return;
        }

        using var file = FileAccess.Open(
            SavePath,
            FileAccess.ModeFlags.Read
        );

        if (file == null)
            return;

        try
        {
            var saveData = JsonSerializer.Deserialize<PlayerSaveData>(
                file.GetAsText()
            );

            if (saveData == null)
                return;

            Data = new PlayerData
            {
                Cash = saveData.Cash,
                Bank = saveData.Bank,
                Level = saveData.Level,
                XP = saveData.XP
            };

            foreach (var path in saveData.UnlockedGunPaths)
            {
                var gun = GD.Load<GunData>(path);

                if (gun != null)
                    Data.UnlockGun(gun);
                else
                    GD.PrintErr("Could not load gun: ", path);
            }
        }
        catch (JsonException)
        {
            GD.PrintErr("Failed to load player data.");
            Data = new PlayerData();
        }
    }
}
