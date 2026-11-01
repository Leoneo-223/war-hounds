
using Godot;
using System;

[GlobalClass]
public partial class NPCData : Resource
{
    // Identity
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int Age { get; set; } = 25;

    // Occupation
    [Export] public string Job { get; set; } = "";
    [Export] public string WorkplaceId { get; set; } = "";
    [Export] public int WorkHours { get; set; } = 8;

    // Personality (0.0 - 1.0)
    [Export(PropertyHint.Range, "0,1")]
    public float Friendliness { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1")]
    public float Aggression { get; set; } = 0.2f;
    [Export(PropertyHint.Range, "0,1")]
    public float Greed { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1")]
    public float Sociability { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0,1")]
    public float Intelligence { get; set; } = 0.5f;

    // Lifestyle
    [Export] public string HomeLocationId { get; set; } = "";
    [Export] public string FavoriteLocationId { get; set; } = "";

    // Appearance
    [Export] public PackedScene CharacterModel { get; set; }
}
