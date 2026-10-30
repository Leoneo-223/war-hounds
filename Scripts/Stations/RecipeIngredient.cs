using Godot;

// One line of a recipe: "2 x Aluminum Foil". Created inline inside a Recipe.
[GlobalClass]
public partial class RecipeIngredient : Resource
{
    [Export] public ItemData Item;
    [Export] public int Count = 1;
}