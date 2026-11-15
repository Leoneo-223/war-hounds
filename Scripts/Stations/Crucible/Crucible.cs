using Godot;

public partial class Crucible : Station
{
    [Export] public PropaneTank Tank;
    [Export] public float BurnRate = 0.5f;
    [Export] public Node3D FlameEffects;

    // FOR TESTING ONLY: when on, a finished recipe is delivered as a pickup right away.
    // Turn it off once you write your own pouring logic.
    [Export] public bool AutoDeliverForTesting = false;

    // Amount of cups the crucible can produce
    [Export] public int CupAmount;

    [Export] public ItemData CrucibleCupEmpty;

    [ExportGroup("OUTPUTS")]
    [Export] public ItemData CrucibleCupAluminum;

    private int _cupsLeft;

    public override void _Ready()
    {
        SetFlame(false);
    }

    // Explains why the crucible can't start, under the normal prompt.
    public override string GetPrompt()
    {
        string prompt = base.GetPrompt();
        if (State != StationState.Idle)
        {
            return prompt;
        }

        if (Tank == null)
        {
            return prompt + "\nNo propane tank connected";
        }

        if (!Tank.ValveOpen)
        {
            return prompt + "\nPropane valve is closed";
        }

        if (!Tank.HasFuel)
        {
            return prompt + "\nPropane tank is empty";
        }

        return prompt;
    }

    public override void Interact()
    {
        if (State == StationState.Done)
        {
            TryFillCup();
            return;
        }

        // Idle / Working, normal start and stop
        base.Interact();
    }

    // Can only start with a connected tank that has its valve open and some fuel left.
    protected override bool CanRun()
    {
        if (Tank == null)
        {
            return false;
        }

        if (!Tank.ValveOpen)
        {
            return false;
        }

        if (!Tank.HasFuel)
        {
            return false;
        }

        return true;
    }

    protected override void OnStarted()
    {
        SetFlame(true);
    }

    // Burn fuel every frame. If the tank can't supply it f (empty, or the valve was
    // closed mid-recipe), the flame dies and the work stops.
    protected override void OnTick(float delta)
    {
        float needed = BurnRate * delta;

        float got = 0f;
        if (Tank != null)
        {
            got = Tank.Consume(needed);
        }

        if (got + 0.00001f < needed)
        {
            Stop();
        }
    }

    protected override void OnStopped()
    {
        SetFlame(false);
    }

    protected override void OnFinished()
    {
        _cupsLeft = CupAmount;

        if (AutoDeliverForTesting)
        {
            Deliver(CompletedRecipe);
            ClearResult();
        }
    }

    // Replaces the empty cup in the player's hand with a full one.
    private void TryFillCup()
    {
        Inventory inv = Inventory.Instance;
        if (inv == null || CrucibleCupEmpty == null)
        {
            return;
        }

        if (inv.SelectedItem != CrucibleCupEmpty)
        {
            return;
        }

        ItemData fullCup = GetFilledCup(CompletedRecipe);
        if (fullCup == null)
        {
            return;
        }

        if (!inv.ReplaceSelected(fullCup))
        {
            return;
        }

        _cupsLeft--;
        if (FlameEffects != null)
        {
            FlameEffects.Position = new Vector3(0f, FlameEffects.Position.Y - 0.1f, 0.01f);
        }

        if (_cupsLeft <= 0)
        {
            ClearResult();
            SetFlame(false);
        }
    }

    private ItemData GetFilledCup(Recipe recipe)
    {
        if (recipe.Name == "Molten Aluminum")
        {
            return CrucibleCupAluminum;
        }
        //else if (recipe.Name == "Molten Gold")
        //{
        //    return CrucibleCupGold;
        //}
        else
        {
            return null;
        }
    }

    private void SetFlame(bool on)
    {
        if (FlameEffects != null)
        {
            FlameEffects.Visible = on;
            FlameEffects.Position = new Vector3(0f, 0.3f, -0.01f);
        }
    }
}
