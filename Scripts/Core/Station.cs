using Godot;

public enum StationState { Idle, Working }

// Base class for every station (crucible, fabricator, packing bench...).
// Handles loading (via ItemIntake), recipe matching, the work timer, the prompt,
// and spawning the finished item. Subclasses add what is specific to them
// (fuel, power, effects) by overriding the hooks at the bottom.
//
// Loading:     physically drop or throw items onto the station's ItemIntake.
// Interaction: E = start / stop
public abstract partial class Station : StaticBody3D, IInteractable
{
    [Export] public string StationName = "Station";

    // Internal storage for what has been dropped in. No UI; the prompt shows what is loaded.
    [Export] public Storage Contents;

    // Where finished items pop out. If empty, they appear at the station's origin.
    [Export] public Marker3D OutputPoint;

    // Everything this station knows how to make.
    [Export] public Godot.Collections.Array<Recipe> Recipes = new();

    public StationState State { get; private set; } = StationState.Idle;

    // 0 to 1, how far along the current recipe is.
    public float Progress { get; private set; }

    protected Recipe CurrentRecipe;

    public virtual string GetPrompt()
    {
        if (State == StationState.Working)
            return $"{StationName} ({Mathf.RoundToInt(Progress * 100)}%)\n[E] Stop";

        string needs = DescribeRequirements();
        return needs == ""
            ? $"[E] Start {StationName}"
            : $"[E] Start {StationName}\n{needs}";
    }

    public virtual void Interact()
    {
        if (State == StationState.Working) Stop();
        else TryStart();
    }

    // Builds lines like "Aluminum Melt: Aluminum Foil 1/2, Scrap 0/1"
    // so the player can see what the station still needs.
    protected string DescribeRequirements()
    {
        if (Contents == null) return "";

        var sb = new System.Text.StringBuilder();
        foreach (Recipe recipe in Recipes)
        {
            if (recipe == null) continue;

            sb.Append(recipe.Name).Append(": ");
            bool first = true;
            foreach (RecipeIngredient ing in recipe.Inputs)
            {
                if (ing?.Item == null) continue;
                int have = Mathf.Min(Contents.CountOf(ing.Item), ing.Count);
                if (!first) sb.Append(", ");
                sb.Append($"{ing.Item.Name} {have}/{ing.Count}");
                first = false;
            }
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    public bool TryStart()
    {
        if (State == StationState.Working) return false;

        CurrentRecipe = FindRecipe();
        if (CurrentRecipe == null) return false;   // required materials not all loaded
        if (!CanRun()) return false;               // e.g. no fuel

        Progress = 0f;
        State = StationState.Working;
        OnStarted();
        return true;
    }

    public void Stop()
    {
        if (State != StationState.Working) return;

        State = StationState.Idle;
        CurrentRecipe = null;
        Progress = 0f;
        OnStopped();
    }

    public override void _Process(double delta)
    {
        if (State != StationState.Working) return;

        float d = (float)delta;
        OnTick(d);
        if (State != StationState.Working) return;   // OnTick may have called Stop()

        Progress += d / Mathf.Max(CurrentRecipe.Time, 0.01f);
        if (Progress >= 1f) Finish();
    }

    private void Finish()
    {
        Recipe done = CurrentRecipe;

        // Ingredients are used up when the work FINISHES, so stopping early loses nothing.
        // Move ConsumeInputs into TryStart() if you want them used up at the start instead.
        ConsumeInputs(done);
        Deliver(done);

        State = StationState.Idle;
        CurrentRecipe = null;
        Progress = 0f;
        OnFinished();
    }

    // Called by the ItemIntake when a dropped item touches it.
    // Returns how many items were taken (0 = refused, the item stays in the world).
    public virtual int Accept(ItemData item, int count)
    {
        if (State == StationState.Working || Contents == null || item == null) return 0;

        // Only take what the recipes actually need, so the station can't be flooded.
        int room = MaxNeeded(item) - Contents.CountOf(item);
        if (room <= 0) return 0;

        int attempt = Mathf.Min(count, room);
        int leftover = Contents.Add(item, attempt);   // Add returns what did NOT fit
        return attempt - leftover;
    }

    // The most of this item any single recipe asks for (0 = not an ingredient at all).
    private int MaxNeeded(ItemData item)
    {
        int max = 0;
        foreach (Recipe recipe in Recipes)
        {
            if (recipe == null) continue;
            foreach (RecipeIngredient ing in recipe.Inputs)
                if (ing?.Item == item) max = Mathf.Max(max, ing.Count);
        }
        return max;
    }

    // The first recipe whose inputs are all loaded, or null.
    protected virtual Recipe FindRecipe()
    {
        if (Contents == null) return null;

        foreach (Recipe recipe in Recipes)
            if (recipe != null && HasAllInputs(recipe)) return recipe;
        return null;
    }

    private bool HasAllInputs(Recipe recipe)
    {
        bool any = false;
        foreach (RecipeIngredient ing in recipe.Inputs)
        {
            if (ing?.Item == null) continue;
            any = true;
            if (Contents.CountOf(ing.Item) < ing.Count) return false;
        }
        return any;   // a recipe with no ingredients never matches
    }

    protected virtual void ConsumeInputs(Recipe recipe)
    {
        foreach (RecipeIngredient ing in recipe.Inputs)
            if (ing?.Item != null) Contents.Remove(ing.Item, ing.Count);
    }

    protected virtual void Deliver(Recipe recipe)
    {
        SpawnPickup(recipe.Output, recipe.OutputCount);
    }

    // Spawns a world pickup at the OutputPoint and pops it out along the marker's -Z.
    protected void SpawnPickup(ItemData item, int count)
    {
        PackedScene scene = Inventory.Instance?.PickupScene;
        if (scene == null || item == null) return;

        var pickup = scene.Instantiate<ItemPickup>();
        pickup.Item = item;
        pickup.Count = count;
        GetTree().CurrentScene.AddChild(pickup);

        Node3D at = this; // default: the station itself
        if (OutputPoint != null) at = OutputPoint; // or the marker, if assigned
        pickup.GlobalPosition = at.GlobalPosition;
        pickup.LinearVelocity = -at.GlobalBasis.Z * 2.5f; // How fast it pops out
    }

    // Extra conditions to start (fuel, power, heat...). Return false to refuse.
    protected virtual bool CanRun() => true;

    protected virtual void OnStarted() { }
    protected virtual void OnTick(float delta) { }   // every frame while working
    protected virtual void OnStopped() { }           // stopped before finishing
    protected virtual void OnFinished() { }          // completed normally
}