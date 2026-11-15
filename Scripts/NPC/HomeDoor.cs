using Godot;

public partial class HomeDoor : StaticBody3D, IInteractable
{
    [Export] public LocationMarker Marker;
    [Export] public string SwitchAction = "switch_resident";
    [Export] public float SwitchRange = 3f;      // how close you must be for the switch key to work
    [Export] public float AnswerDelay = 1.5f;    // seconds between knocking and the door opening
    [Export] public AudioStreamPlayer3D KnockSound;

    private int _selected;
    private bool _knocking;
    private NPC _answering;
    private Node3D _player;

    private Node3D Player
    {
        get
        {
            if (_player == null || !IsInstanceValid(_player))
                _player = GetTree().GetFirstNodeInGroup("player") as Node3D;
            return _player;
        }
    }

    private bool SomeoneAtDoor => _answering != null && IsInstanceValid(_answering) && _answering.IsAnsweringDoor;

    public override void _UnhandledInput(InputEvent e)
    {
        if (Marker == null || !e.IsActionPressed(SwitchAction)) return;
        if (Marker.Inside.Count < 2 || _knocking || SomeoneAtDoor) return;

        var p = Player;
        if (p == null || p.GlobalPosition.DistanceTo(GlobalPosition) > SwitchRange) return;

        _selected = (_selected + 1) % Marker.Inside.Count;
        GetViewport().SetInputAsHandled();
    }

    private NPC PickResident()
    {
        if (Marker == null || Marker.Inside.Count == 0) return null;
        _selected = Mathf.Clamp(_selected, 0, Marker.Inside.Count - 1);
        return Marker.Inside[_selected];
    }

    public string GetPrompt()
    {
        if (Marker == null || _knocking || SomeoneAtDoor) return "";

        var npc = PickResident();
        if (npc == null) return "Knock";

        string name = npc.Data?.DisplayName ?? "someone";
        return Marker.Inside.Count > 1
            ? $"Knock for {name}  [F: switch]"
            : $"Knock for {name}";
    }

    public async void Interact()
    {
        if (Marker == null || _knocking || SomeoneAtDoor) return;

        _knocking = true;
        NPC target = PickResident(); // decided now, so switching mid-wait changes nothing
        KnockSound?.Play();

        await ToSignal(GetTree().CreateTimer(AnswerDelay), SceneTreeTimer.SignalName.Timeout);
        _knocking = false;

        // Nobody home (or they left meanwhile): no answer.
        if (target == null || !IsInstanceValid(target) || !target.IsInside) return;

        _answering = target;
        target.AnswerDoor(Player);
    }
}