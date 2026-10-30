using Godot;

// The first station. Loading, recipes, timer and output come from Station.
// This class adds what is specific to a crucible: it burns propane while it works.
public partial class Crucible : Station
{
    // The tank sitting next to it. Drag the PropaneTank node into this slot.
    [Export] public PropaneTank Tank;

    // Fuel used per second while running. The default 0.5 means an 8 second recipe uses 4.
    [Export] public float BurnRate = 0.5f;

    [Export] public Node3D MoltenMetal;

    public override void _Ready()
    {
        MoltenMetal.Visible = false;
    }

    // Explains why the crucible can't start, under the normal prompt.
    public override string GetPrompt()
    {
        string prompt = base.GetPrompt();
        if (State == StationState.Working) return prompt;

        if (Tank == null) return prompt + "\nNo propane tank connected";
        if (!Tank.ValveOpen) return prompt + "\nPropane valve is closed";
        if (!Tank.HasFuel) return prompt + "\nPropane tank is empty";
        return prompt;
    }

    // Can only start with a connected tank that has its valve open and some fuel left.
    protected override bool CanRun() => Tank != null && Tank.ValveOpen && Tank.HasFuel;

    // Burn fuel every frame. If the tank can't supply it all (empty, or the valve was
    // closed mid-recipe), the flame dies and the work stops.
    protected override void OnTick(float delta)
    {
        float needed = BurnRate * delta;
        float got = Tank != null ? Tank.Consume(needed) : 0f;

        if (got + 0.00001f < needed) Stop();
    }

    protected override void OnStarted()
    {
        if (MoltenMetal != null) MoltenMetal.Visible = true;
    }

    protected override void OnFinished()
    {
        if (MoltenMetal != null) MoltenMetal.Visible = false;
    }

    protected override void OnStopped()
    {
        if (MoltenMetal != null) MoltenMetal.Visible = false;
    }
}