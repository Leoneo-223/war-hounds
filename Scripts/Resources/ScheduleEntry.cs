using Godot;

public enum NPCActivity { Idle, Work, Sleep, Social, Wander }

// One line of an NPC's daily routine: "from this time, be at this place doing this".
[GlobalClass]
public partial class ScheduleEntry : Resource
{
    [Export(PropertyHint.Range, "0,23")] public int StartHour { get; set; } = 8;
    [Export(PropertyHint.Range, "0,59")] public int StartMinute { get; set; } = 0;
    [Export] public string LocationId { get; set; } = "";
    [Export] public NPCActivity Activity { get; set; } = NPCActivity.Idle;

    public int StartMinuteOfDay => StartHour * 60 + StartMinute;
}