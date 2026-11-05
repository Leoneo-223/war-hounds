using Godot;

// AUTOLOAD SCRIPT
public partial class GameClock : Node
{
    public static GameClock Instance { get; private set; }

    [Signal] public delegate void MinuteChangedEventHandler(int minuteOfDay);

    // 1 real second = 1 game minute -> a full day lasts 24 real minutes.
    [Export] public float RealSecondsPerGameMinute = 1f;
    [Export] public int StartMinute = 6 * 60;

    public int MinuteOfDay { get; private set; }
    public int Day { get; private set; } = 1;

    private double _accum;

    public override void _Ready()
    {
        Instance = this;
        MinuteOfDay = StartMinute;
    }

    public override void _Process(double delta)
    {
        _accum += delta;
        while (_accum >= RealSecondsPerGameMinute)
        {
            _accum -= RealSecondsPerGameMinute;
            MinuteOfDay++;
            if (MinuteOfDay >= 1440)
            {
                MinuteOfDay = 0;
                Day++;
            }
            EmitSignal(SignalName.MinuteChanged, MinuteOfDay);
        }
    }
}