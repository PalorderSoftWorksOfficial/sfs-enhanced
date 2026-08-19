using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFS.World;

public class WorldParticle : MonoBehaviour
{
	public ParticleSystem effect;

	public float drag;

	public bool gas;

	private static Dictionary<WorldParticle, WorldParticle> instances = new Dictionary<WorldParticle, WorldParticle>();

	public void Spawn((Vector3, Vector3)[] particles)
	{
		ParticleSystem particleSystem = GetInstance().effect;
		bool value = WorldTime.main.realtimePhysics.Value;
		for (int i = 0; i < particles.Length; i++)
		{
			(Vector3, Vector3) tuple = particles[i];
			Vector3 item = tuple.Item1;
			Vector3 item2 = tuple.Item2;
			ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
			{
				position = item,
				velocity = item2 * (value ? (1f + Time.fixedDeltaTime) : 1f)
			};
			particleSystem.Emit(emitParams, 1);
		}
	}

	public void Spawn(ParticleSystem.EmitParams[] particles)
	{
		ParticleSystem particleSystem = GetInstance().effect;
		foreach (ParticleSystem.EmitParams emitParams in particles)
		{
			particleSystem.Emit(emitParams, 1);
		}
	}

	private WorldParticle GetInstance()
	{
		if (!instances.ContainsKey(this))
		{
			if (instances.ContainsValue(this))
			{
				throw new Exception("Your trying to clone a instance");
			}
			WorldParticle value = UnityEngine.Object.Instantiate(this);
			instances.Add(this, value);
			SceneManager.sceneUnloaded += delegate
			{
				instances.Remove(this);
			};
		}
		return instances[this];
	}

	private void Start()
	{
		WorldView main = WorldView.main;
		main.onPositionOffset = (Action<Vector2>)Delegate.Combine(main.onPositionOffset, new Action<Vector2>(OnPositionOffset));
		WorldView main2 = WorldView.main;
		main2.onVelocityOffset = (Action<Vector2>)Delegate.Combine(main2.onVelocityOffset, new Action<Vector2>(OnVelocityOffset));
		ParticleSystem.MainModule main3 = effect.main;
		main3.simulationSpeed = 0.001f;
	}

	private void Update()
	{
		if (!WorldTime.main.realtimePhysics)
		{
			Simulate(Time.deltaTime * (float)WorldTime.main.timewarpSpeed);
			OnPositionOffset(WorldView.main.velocityOffset.Value * Time.deltaTime * (float)WorldTime.main.timewarpSpeed);
		}
	}

	private void FixedUpdate()
	{
		if ((bool)WorldTime.main.realtimePhysics)
		{
			Simulate(Time.fixedDeltaTime);
		}
	}

	private void Simulate(float deltaTime)
	{
		Apply(delegate(ParticleSystem.Particle[] particles)
		{
			if (!gas || WorldView.main.ViewLocation.planet.GetAtmosphericDensity(WorldView.main.ViewLocation.Height) == 0.0)
			{
				Vector3 vector = WorldView.main.ViewLocation.planet.GetGravity(WorldView.main.ViewLocation.position) * deltaTime;
				for (int i = 0; i < particles.Length; i++)
				{
					particles[i].velocity += vector;
				}
			}
			for (int j = 0; j < particles.Length; j++)
			{
				particles[j].position += particles[j].velocity * deltaTime;
			}
			for (int k = 0; k < particles.Length; k++)
			{
				particles[k].remainingLifetime -= deltaTime;
			}
			if (drag > 0f)
			{
				Vector3 vector2 = WorldView.main.velocityOffset.Value;
				float num = (float)WorldView.main.ViewLocation.planet.GetAtmosphericDensity(WorldView.main.ViewLocation.Height) * deltaTime * drag;
				for (int l = 0; l < particles.Length; l++)
				{
					Vector3 vector3 = vector2 + particles[l].velocity;
					particles[l].velocity -= vector3.normalized * (vector3.sqrMagnitude * num);
				}
			}
			for (int m = 0; m < particles.Length; m++)
			{
				particles[m].rotation = Mathf.Atan2(particles[m].velocity.y, particles[m].velocity.x) * 57.29578f;
			}
		});
	}

	private void OnPositionOffset(Vector2 offset)
	{
		Vector3 offset_V3 = offset;
		Apply(delegate(ParticleSystem.Particle[] particles)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].position += offset_V3;
			}
		});
	}

	private void OnVelocityOffset(Vector2 offset)
	{
		Vector3 offset_V3 = offset;
		Apply(delegate(ParticleSystem.Particle[] particles)
		{
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i].velocity += offset_V3;
			}
		});
	}

	private void Apply(Action<ParticleSystem.Particle[]> action)
	{
		ParticleSystem.Particle[] array = new ParticleSystem.Particle[effect.particleCount];
		int particles = effect.GetParticles(array);
		action(array);
		effect.SetParticles(array, particles);
	}
}
