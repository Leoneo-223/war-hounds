using Godot;

public static class MagazineLogic
{
    public static void Initialize(PartData part)
    {
        GD.Print($"Magazine: {part.Name}");

        // Magazine-specific logic
    }
}