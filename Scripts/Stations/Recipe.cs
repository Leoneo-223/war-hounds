using Godot;
using Godot.Collections;

// Data for one thing a station can make. Create these as .tres files and add them
// to a station's Recipes list. No logic here, stations read it.
[GlobalClass]
public partial class Recipe : Resource
{
    [Export] public string Name = "Recipe";

    // Everything that has to be loaded into the station to start.
    [Export] public Array<RecipeIngredient> Inputs = new Array<RecipeIngredient>();

    [Export] public ItemData Output;
    [Export] public int OutputCount = 1;

    // How long the work takes, in seconds.
    [Export] public float Time = 5f;
}
