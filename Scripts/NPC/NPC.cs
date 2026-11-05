using Godot;

public partial class NPC : CharacterBody3D, IInteractable
{
    [ExportGroup("DATA")]
    [Export] public NPCData Data;

    [ExportGroup("MOVEMENT")]
    [Export] public float WalkSpeed = 1.6f;
    [Export] public float TurnSpeed = 6f;

    [ExportGroup("NODES")]
    [Export] public NavigationAgent3D Agent;
    [Export] public Godot.Collections.Array<Marker3D> Waypoints = new();

    public NPCActivity CurrentActivity { get; private set; } = NPCActivity.Idle;

    // Set true while talking / trading so the NPC stops walking and turning.
    public bool IsBusy { get; set; }

    // True while the NPC is "inside" a building (invisible, no collision).
    public bool IsInside => _inside;

    private ScheduleEntry _currentEntry;
    private LocationMarker _targetMarker;
    private bool _arrived = true;

    // Arrival facing (non-home markers)
    private bool _faceOnArrival;
    private float _arrivalYaw;

    // Inside-a-building state
    private bool _inside;
    private uint _savedLayer, _savedMask;

    public override async void _Ready()
    {
        if (Data?.CharacterModel != null)
        {
            // Swap the placeholder capsule for the real model
            GetNodeOrNull<MeshInstance3D>("MeshInstance3D")?.QueueFree();
            AddChild(Data.CharacterModel.Instantiate());
        }

        // The navigation map isn't ready until after the first physics frame.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        if (GameClock.Instance == null) return;
        GameClock.Instance.MinuteChanged += OnMinuteChanged;
        ApplySchedule(GameClock.Instance.MinuteOfDay, teleport: true);
    }

    public override void _ExitTree()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.MinuteChanged -= OnMinuteChanged;
    }

    private void OnMinuteChanged(int minuteOfDay) => ApplySchedule(minuteOfDay, teleport: false);

    private ScheduleEntry GetEntryFor(int minute)
    {
        if (Data == null || Data.Schedule.Count == 0) return null;

        ScheduleEntry best = null, latest = null;
        foreach (var e in Data.Schedule)
        {
            if (e == null) continue; // skip empty slots

            if (latest == null || e.StartMinuteOfDay > latest.StartMinuteOfDay) latest = e;
            if (e.StartMinuteOfDay <= minute && (best == null || e.StartMinuteOfDay > best.StartMinuteOfDay))
                best = e;
        }
        // Before the first entry of the day = still doing last night's final entry.
        return best ?? latest;
    }

    private void ApplySchedule(int minute, bool teleport)
    {
        var entry = GetEntryFor(minute);
        if (entry == null || entry == _currentEntry) return;

        var marker = LocationMarker.Find(entry.LocationId);
        if (marker == null)
        {
            GD.PushWarning($"{Data?.DisplayName}: no LocationMarker with id '{entry.LocationId}'");
            return;
        }

        _currentEntry = entry;
        _targetMarker = marker;
        CurrentActivity = entry.Activity;
        _faceOnArrival = false;

        // Leaving a building: reappear where we went in (the door).
        if (_inside) ComeOutside();

        if (teleport)
        {
            GlobalPosition = marker.GlobalPosition;
            Arrive(snapFacing: true);
        }
        else
        {
            Agent.TargetPosition = marker.GlobalPosition;
            _arrived = false;
        }
    }

    private void Arrive(bool snapFacing)
    {
        _arrived = true;
        if (_targetMarker == null) return;

        if (_targetMarker.HidesNpc)
        {
            GoInside();
            return;
        }

        _arrivalYaw = _targetMarker.FacingYaw;
        if (snapFacing)
        {
            Rotation = new Vector3(0, _arrivalYaw, 0);
            _faceOnArrival = false;
        }
        else
        {
            _faceOnArrival = true;
        }
    }

    private void GoInside()
    {
        if (_inside) return;
        _inside = true;
        _savedLayer = CollisionLayer;
        _savedMask = CollisionMask;
        CollisionLayer = 0;
        CollisionMask = 0;
        Velocity = Vector3.Zero;
        Visible = false;
    }

    private void ComeOutside()
    {
        if (!_inside) return;
        _inside = false;
        CollisionLayer = _savedLayer;
        CollisionMask = _savedMask;
        Visible = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_inside) return; // nothing to simulate while indoors

        Vector3 horizontal = Vector3.Zero;

        if (!_arrived && !IsBusy)
        {
            if (Agent.IsNavigationFinished())
            {
                Arrive(snapFacing: false);
            }
            else
            {
                Vector3 dir = Agent.GetNextPathPosition() - GlobalPosition;
                dir.Y = 0;
                if (dir.LengthSquared() > 0.0001f)
                {
                    dir = dir.Normalized();
                    horizontal = dir * WalkSpeed;
                    TurnTowardsYaw(Mathf.Atan2(-dir.X, -dir.Z), (float)delta);
                }
            }
        }
        else if (_arrived && _faceOnArrival && !IsBusy)
        {
            TurnTowardsYaw(_arrivalYaw, (float)delta);
            if (Mathf.Abs(Mathf.Wrap(Rotation.Y - _arrivalYaw, -Mathf.Pi, Mathf.Pi)) < 0.01f)
                _faceOnArrival = false;
        }

        float y = IsOnFloor() ? 0f : Velocity.Y + GetGravity().Y * (float)delta;
        Velocity = new Vector3(horizontal.X, y, horizontal.Z);
        MoveAndSlide();
    }

    private void TurnTowardsYaw(float targetYaw, float delta)
    {
        float yaw = Mathf.LerpAngle(Rotation.Y, targetYaw, TurnSpeed * delta);
        Rotation = new Vector3(0, yaw, 0);
    }

    // IInteractable
    public virtual string GetPrompt() => Data != null ? $"Talk to {Data.DisplayName}" : "";

    public virtual void Interact()
    {
        // TODO: open dialogue / trade UI, set IsBusy = true while it's open
    }
}