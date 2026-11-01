using Godot;

// A separate world object that holds fuel. The Crucible gets a reference to it
// through the Inspector, so it can be moved or swapped in the scene freely.
//
// E on the tank:
//   holding a refill item (and the tank isn't full) -> uses one up and adds fuel
//   anything else                                    -> opens/closes the valve
public partial class PropaneTank : StaticBody3D, IInteractable
{
    [Export] public string TankName = "Propane Tank";
    [Export] public float MaxFuel = 100f;
    [Export] public float Fuel = 100f;
    [Export] public bool ValveOpen = false;

    // The item that refills this tank (for example a Propane Canister ItemData).
    [Export] public ItemData RefillItem;

    // How much fuel one refill item adds.
    [Export] public float RefillAmount = 50f;

    public bool HasFuel
    {
        get
        {
            return Fuel > 0f;
        }
    }

    private int FuelPercent
    {
        get
        {
            return Mathf.RoundToInt(Fuel / MaxFuel * 100f);
        }
    }

    // True when the player is holding the refill item and there is room for fuel.
    private bool CanRefill
    {
        get
        {
            if (RefillItem == null)
            {
                return false;
            }

            if (Inventory.Instance == null)
            {
                return false;
            }

            if (Inventory.Instance.SelectedItem != RefillItem)
            {
                return false;
            }

            if (Fuel >= MaxFuel)
            {
                return false;
            }

            return true;
        }
    }

    public string GetPrompt()
    {
        if (CanRefill)
        {
            return $"[E] Refill with {RefillItem.Name} ({FuelPercent}%)";
        }

        if (ValveOpen)
        {
            return $"[E] Close valve ({FuelPercent}%)";
        }
        else
        {
            return $"[E] Open valve ({FuelPercent}%)";
        }
    }

    public void Interact()
    {
        if (CanRefill)
        {
            // Use up one item from the held slot, then add its fuel (capped at MaxFuel).
            Inventory inv = Inventory.Instance;
            inv.RemoveFromSlot(inv.Selected, 1);
            Fuel = Mathf.Min(MaxFuel, Fuel + RefillAmount);
            return;
        }

        ValveOpen = !ValveOpen;
    }

    // Takes up to 'amount' of fuel and returns how much was actually given.
    // Gives nothing while the valve is closed, and less than asked when the tank runs dry.
    public float Consume(float amount)
    {
        if (!ValveOpen || Fuel <= 0f || amount <= 0f)
        {
            return 0f;
        }

        float given = Mathf.Min(amount, Fuel);
        Fuel -= given;
        return given;
    }
}
