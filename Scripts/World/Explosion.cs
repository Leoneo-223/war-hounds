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
		else
		{
			GD.PushWarning("Explosion: DebrisParticles not assigned");
		}

		if (FireParticles != null)
		{
			FireParticles.Emitting = true;
		}
		else
		{
			GD.PushWarning("Explosion: FireParticles not assigned");
		}

		if (SmokeParticles != null)
		{
			SmokeParticles.Emitting = true;
		}
		else
		{
			GD.PushWarning("Explosion: SmokeParticles not assigned");
		}

		if (ExplosionSound != null)
		{
			ExplosionSound.Play();
		}
		else
		{
			GD.PushWarning("Explosion: ExplosionSound not assigned");
		}

		// Remove the explosion once everything has finished
		GetTree().CreateTimer(Lifetime).Timeout += OnLifetimeEnd;
	}

	private void OnLifetimeEnd()
	{
		QueueFree();
	}
}