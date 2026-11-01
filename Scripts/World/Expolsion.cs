using Godot;

public partial class Explosion : Node3D
{
	[Export] public GpuParticles3D DebrisParticles;
	[Export] public GpuParticles3D FireParticles;
	[Export] public GpuParticles3D SmokeParticles;
	[Export] public AudioStreamPlayer3D ExplosionSound;
	[Export] public float Lifetime = 1f;

	public override void _Ready()
	{
		if (DebrisParticles != null)
		{
			DebrisParticles.Emitting = true;
		}

		if (FireParticles != null)
		{
			FireParticles.Emitting = true;
		}

		if (SmokeParticles != null)
		{
			SmokeParticles.Emitting = true;
		}

		if (ExplosionSound != null)
		{
			ExplosionSound.Play();
		}

		// Remove the explosion once everything has finished
		GetTree().CreateTimer(Lifetime).Timeout += OnLifetimeEnd;
	}

	private void OnLifetimeEnd()
	{
		QueueFree();
	}
}