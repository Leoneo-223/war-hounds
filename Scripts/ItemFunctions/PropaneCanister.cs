using Godot;

public partial class PropaneCanister : Node3D
{
    [Export] public PackedScene ExplosionScene;
    [Export] public float ImpactSpeed = 6f;

    private RigidBody3D _body;
    private Vector3 _lastVelocity;
    private bool _exploded;
    private Player _player;

    public override void _Ready()
    {
        // Look upward for a RigidBody3D (the dropped pickup).
        // In the player's hand there isn't one, so nothing happens.
        Node current = GetParent();
        while (current != null && !(current is RigidBody3D))
        {
            current = current.GetParent();
        }

        _body = current as RigidBody3D;

        if (_body == null)
        {
            SetPhysicsProcess(false);
            return;
        }

        _body.ContactMonitor = true;
        _body.MaxContactsReported = 4;
        _body.BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        _lastVelocity = _body.LinearVelocity;
    }

    private void OnBodyEntered(Node body)
    {
        if (_exploded)
        {
            return;
        }

        float speed = _lastVelocity.Length();
        if (speed < ImpactSpeed)
        {
            return;
        }

        _exploded = true;
        CallDeferred(nameof(Explode));
    }

    private void Explode()
    {
        if (ExplosionScene != null)
        {
            Node3D explosion = ExplosionScene.Instantiate<Node3D>();
            GetTree().CurrentScene.AddChild(explosion);
            explosion.GlobalPosition = _body.GlobalPosition;


        }

        _body.QueueFree();
    }
}