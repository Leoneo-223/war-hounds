// Tiny global flag shared by systems that need to know a menu is open.
// While UiOpen is true, movement, looking, hotbar keys and interaction are ignored.
public static class GameState
{
    public static bool UiOpen;
}
