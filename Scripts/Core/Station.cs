using Godot;
using Godot.Collections;
using System;
using System.Text;

public enum StationState { Idle, Working, Done }

// Base class for every station (crucible, fabricator, packing bench...).
// Handles loading (via ItemIntake), recipe matching, the work timer and the prompt.
// It does NOT hand over the result by itself. When a recipe finishes, the station
// goes to Done and remembers what it made (CompletedRecipe). The subclass decides
// when and how to deliver: call Deliver() / SpawnPickup(), then ClearResult().
public abstract partial class Station : StaticBody3D, IInteractable
{
    [Export] public string StationName = "Station";

    // Internal storage for what has been dropped in. No UI because the prompt shows what is loaded.
    [Export] public Storage Contents;

    // Default spawn point for SpawnPickup. If empty, items appear at the station origin.
    [Export] public Marker3D OutputPoint;

    // Everything this station knows how to make.
    [Export] public Array<Recipe> Recipes = new Array<Recipe>();

    public StationState State { get; private set; } = StationState.Idle;

    // 0 to 1, how far along the current recipe is.
    public float Progress { get; private set; }

    // The recipe being worked on right now (null unless Working).
    protected Recipe CurrentRecipe;

    // The recipe that just finished and is waiting to be delivered (null unless Done).
    protected Recipe CompletedRecipe { get; private set; }

    public virtual string GetPrompt()
    {
        switch (State)
        {
            case StationState.Working:
                {
                    return $"{StationName} ({Mathf.RoundToInt(Progress * 100)}%)\n[E] Stop";
                }

            case StationState.Done:
                {
                    string recipeName = "";
                    if (CompletedRecipe != null)
                    {
                        recipeName = CompletedRecipe.Name;
                    }
                    return $"{StationName}: {recipeName} ready";
                }

            default:
                {
                    string needs = DescribeRequirements();
                    if (needs == "")
                    {
                        return $"[E] Start {StationName}";
                    }
                    return $"[E] Start {StationName}\n{needs}";
                }
        }
    }

    public virtual void Interact()
    {
        // In the Done state nothing happens by default. Override Interact in your
        // station script to add the "take the result" action (pour, grab, etc.).
        if (State == StationState.Working)
        {
            Stop();
        }
        else if (State == StationState.Idle)
        {
            TryStart();
        }
    }

    // Builds lines like "Aluminum Melt: Aluminum Foil 1/2, Scrap 0/1"
    // so the player can see what the station still needs.
    protected string DescribeRequirements()
    {
        if (Contents == null)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        foreach (Recipe recipe in Recipes)
        {
            if (recipe == null)
            {
                continue;
            }

            sb.Append(recipe.Name).Append(": ");
            bool first = true;
            foreach (RecipeIngredient ing in recipe.Inputs)
            {
                if (ing == null || ing.Item == null)
                {
                    continue;
                }

                int have = Mathf.Min(Contents.CountOf(ing.Item), ing.Count);
                if (!first)
                {
                    sb.Append(", ");
                }
                sb.Append($"{ing.Item.Name} {have}/{ing.Count}");
                first = false;
            }
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    public bool TryStart()
    {
        // These are checks that determine if the station can't run
        // if one of them checks out it returns (false) 
        if (State != StationState.Idle)
        {
            return false;   // busy, or a result is still waiting
        }

        CurrentRecipe = FindRecipe();
        if (CurrentRecipe == null)
        {
            return false;   // required materials not all loaded
        }

        if (!CanRun())
        {
            return false;   // e.g. no fuel
        }

        Progress = 0f;
        State = StationState.Working;
        OnStarted();
        return true;
    }

    public void Stop()
    {
        if (State != StationState.Working)
        {
            return;
        }

        State = StationState.Idle;
        CurrentRecipe = null;
        Progress = 0f;
        OnStopped();
    }

    public override void _Process(double delta)
    {
        if (State != StationState.Working)
        {
            return;
        }

        float d = (float)delta;
        OnTick(d);

        if (State != StationState.Working)
        {
            return;   // OnTick may have called Stop()
        }

        Progress += d / Mathf.Max(CurrentRecipe.Time, 0.01f);
        if (Progress >= 1f)
        {
            Finish();
        }
    }

    private void Finish()
    {
        Recipe done = CurrentRecipe;

        // Ingredients are used up when the work FINISHES, so stopping early loses nothing.
        // Move ConsumeInputs into TryStart() if you want them used up at the start instead.
        ConsumeInputs(done);

        // Nothing is delivered here. The station now holds a result until
        // the subclass delivers it and calls ClearResult().
        CurrentRecipe = null;
        CompletedRecipe = done;
        Progress = 0f;
        State = StationState.Done;
        OnFinished();
    }

    // Call this once the result has been taken or delivered. The station goes back to Idle
    // and can start a new batch.
    protected void ClearResult()
    {
        CompletedRecipe = null;
        if (State == StationState.Done)
        {
            State = StationState.Idle;
        }
    }

    // Called by the ItemIntake when a dropped item touches it.
    // Returns how many items were taken (0 = refused, the item stays in the world).
    public virtual int Accept(ItemData item, int count)
    {
        if (State != StationState.Idle || Contents == null || item == null)
        {
            return 0;
        }

        // Only take what the recipes actually need, so the station can't be flooded.
        int room = MaxNeeded(item) - Contents.CountOf(item);
        if (room <= 0)
        {
            return 0;
        }

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
            if (recipe == null)
            {
                continue;
            }

            foreach (RecipeIngredient ing in recipe.Inputs)
            {
                if (ing != null && ing.Item == item)
                {
                    max = Mathf.Max(max, ing.Count);
                }
            }
        }
        return max;
    }

    // The first recipe whose inputs are all loaded, or null.
    protected virtual Recipe FindRecipe()
    {
        if (Contents == null)
        {
            return null;
        }

        foreach (Recipe recipe in Recipes)
        {
            if (recipe != null && HasAllInputs(recipe))
            {
                return recipe;
            }
        }
        return null;
    }

    private bool HasAllInputs(Recipe recipe)
    {
        bool any = false;
        foreach (RecipeIngredient ing in recipe.Inputs)
        {
            if (ing == null || ing.Item == null)
            {
                continue;
            }

            any = true;
            if (Contents.CountOf(ing.Item) < ing.Count)
            {
                return false;
            }
        }
        return any;   // a recipe with no ingredients never matches
    }

    protected virtual void ConsumeInputs(Recipe recipe)
    {
        foreach (RecipeIngredient ing in recipe.Inputs)
        {
            if (ing != null && ing.Item != null)
            {
                Contents.Remove(ing.Item, ing.Count);
            }
        }
    }

    // Spawns the recipe's result as a pickup at the OutputPoint. Call it yourself
    // from a station script when you decide the result should come out.
    // It does not change the station's state, so call ClearResult() afterwards.
    protected virtual void Deliver(Recipe recipe)
    {
        SpawnPickup(recipe.Output, recipe.OutputCount);
    }

    // Spawns any item as a pickup and pops it out along the point's -Z.
    // 'at' is optional: pass a different Marker3D (for example above a mold) to spawn there.
    protected void SpawnPickup(ItemData item, int count, Node3D at = null)
    {
        PackedScene scene = null;
        if (Inventory.Instance != null)
        {
            scene = Inventory.Instance.PickupScene;
        }

        if (scene == null || item == null)
        {
            return;
        }

        ItemPickup pickup = scene.Instantiate<ItemPickup>();
        pickup.Item = item;
        pickup.Count = count;
        GetTree().CurrentScene.AddChild(pickup);

        Node3D spawnAt = at;                                  // the point passed in
        if (spawnAt == null)
        {
            spawnAt = OutputPoint;                            // otherwise the default marker
        }
        if (spawnAt == null)
        {
            spawnAt = this;                                   // otherwise the station itself
        }

        pickup.GlobalPosition = spawnAt.GlobalPosition;
        pickup.LinearVelocity = -spawnAt.GlobalBasis.Z * 2.5f;   // how fast it pops out
    }

    // Extra conditions to start (fuel, power, heat...). Return false to refuse.
    protected virtual bool CanRun()
    {
        return true;
    }

    protected virtual void OnStarted()
    {
    }

    // Every frame while working.
    protected virtual void OnTick(float delta)
    {
    }

    // Stopped before finishing.
    protected virtual void OnStopped()
    {
    }

    // Recipe done, State is now Done.
    protected virtual void OnFinished()
    {
    }
}
