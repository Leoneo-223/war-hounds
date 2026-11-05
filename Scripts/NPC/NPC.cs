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

    // Set true while talking / trading so the NPC stops walking.
    public bool IsBusy { get; set; }

    private ScheduleEntry _currentEntry;
    private bool _arrived = true;

    public override async void _Ready()
    {
        if (Data?.CharacterModel != null)
            AddChild(Data.CharacterModel.Instantiate());

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
        CurrentActivity = entry.Activity;

        if (teleport)
        {
            GlobalPosition = marker.GlobalPosition;
            _arrived = true;
        }
        else
        {
            Agent.TargetPosition = marker.GlobalPosition;
            _arrived = false;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 horizontal = Vector3.Zero;

        if (!_arrived && !IsBusy)
        {
            if (Agent.IsNavigationFinished())
            {
                _arrived = true;
            }
            else
            {
                Vector3 dir = Agent.GetNextPathPosition() - GlobalPosition;
                dir.Y = 0;
                if (dir.LengthSquared() > 0.0001f)
                {
                    dir = dir.Normalized();
                    horizontal = dir * WalkSpeed;
                    FaceDirection(dir, (float)delta);
                }
            }
        }

        float y = IsOnFloor() ? 0f : Velocity.Y + GetGravity().Y * (float)delta;
        Velocity = new Vector3(horizontal.X, y, horizontal.Z);
        MoveAndSlide();
    }

    private void FaceDirection(Vector3 dir, float delta)
    {
        float targetYaw = Mathf.Atan2(-dir.X, -dir.Z);
        float yaw = Mathf.LerpAngle(Rotation.Y, targetYaw, TurnSpeed * delta);
        Rotation = new Vector3(0, yaw, 0);
    }

    // IInteractable
    public virtual string GetPrompt() => Data != null ? $"[E] Talk to {Data.DisplayName}" : "";

    public virtual void Interact()
    {
        // TODO: open dialogue / trade UI, set IsBusy = true while it's open
    }
}