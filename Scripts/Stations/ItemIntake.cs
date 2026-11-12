using Godot;

// Area3D settings are Collision Layer = nothing, Collision Mask = layers 1 and 2
// (layer 2 is where ItemPickup puts itself, layer 1 is where the Player is).
public partial class ItemIntake : Area3D
{
    [ExportGroup("Player Damage")]
    [Export] public bool HurtsPlayer = true;
    [Export] public int DamagePerTick = 10;
    [Export] public float TickInterval = 0.5f;
    [Export] public bool OnlyWhileWorking = false;

    private Station _station;
    private float _timer;
    private float _damageTimer;
    private Player _player; // null while the player is outside the area

    public bool PlayerInside
    {
        get
        {
            return _player != null;
        }
    }

    public override void _Ready()
    {
        _station = GetParentOrNull<Station>();
        if (_station == null)
        {
            GD.PushError("ItemIntake must be a direct child of a Station node.");
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node3D body)
    {
        Player p = body as Player;
        if (p != null)
        {
            _player = p;
            _damageTimer = 0f; // first hit lands right after TickInterval
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body == _player)
        {
            _player = null;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_station == null)
        {
            return;
        }

        HurtPlayer((float)delta);

        // Check a few times per second, not every frame. Polling (instead of BodyEntered)
        // also catches items that were refused earlier and are still sitting here.
        _timer += (float)delta;
        if (_timer < 0.2f)
        {
            return;
        }
        _timer = 0f;

        foreach (Node3D body in GetOverlappingBodies())
        {
            ItemPickup pickup = body as ItemPickup;
            if (pickup == null)
            {
                continue;
            }

            if (pickup.IsQueuedForDeletion() || pickup.Item == null)
            {
                continue;
            }

            int taken = _station.Accept(pickup.Item, pickup.Count);
            if (taken <= 0)
            {
                continue;
            }

            pickup.Count -= taken;
            if (pickup.Count <= 0)
            {
                pickup.QueueFree();
            }
        }
    }

    private void HurtPlayer(float delta)
    {
        if (!HurtsPlayer || _player == null || _player.IsDead)
        {
            return;
        }

        if (OnlyWhileWorking && _station.State != StationState.Working)
        {
            return;
        }

        _damageTimer += delta;
        if (_damageTimer < TickInterval)
        {
            return;
        }
        _damageTimer = 0f;

        _player.Damage(DamagePerTick);
    }
}
