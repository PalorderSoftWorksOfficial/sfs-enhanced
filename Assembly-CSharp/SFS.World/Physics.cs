using System;
using System.Linq;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class Physics : MonoBehaviour
{
	public WorldLocation location;

	public WorldLoader loader;

	public Trajectory trajectory;

	private I_Physics savedObject;

	public I_Physics PhysicsObject
	{
		get
		{
			if (savedObject == null)
			{
				savedObject = GetComponent<I_Physics>();
			}
			return savedObject;
		}
	}

	public bool PhysicsMode
	{
		get
		{
			if (PhysicsObject != null)
			{
				return PhysicsObject.PhysicsMode;
			}
			return false;
		}
		set
		{
			if (PhysicsMode != value)
			{
				if (value)
				{
					location.Value = trajectory.GetLocation(WorldTime.main.worldTime);
					PhysicsObject.LocalPosition = WorldView.ToLocalPosition(location.position);
					PhysicsObject.LocalVelocity = WorldView.ToLocalVelocity(location.velocity);
					PhysicsObject.PhysicsMode = true;
					trajectory = null;
				}
				else
				{
					location.position.Value = WorldView.ToGlobalPosition(PhysicsObject.LocalPosition);
					location.velocity.Value = WorldView.ToGlobalVelocity(PhysicsObject.LocalVelocity);
					PhysicsObject.PhysicsMode = false;
					trajectory = Trajectory.CreateTrajectory(location.Value);
				}
			}
		}
	}

	public void SetLocationAndState(Location newLocation, bool physicsMode)
	{
		if (physicsMode)
		{
			location.Value = newLocation;
			PhysicsObject.LocalPosition = WorldView.ToLocalPosition(location.position);
			PhysicsObject.LocalVelocity = WorldView.ToLocalVelocity(location.velocity);
			PhysicsObject.PhysicsMode = true;
			trajectory = null;
		}
		else
		{
			location.Value = newLocation;
			PhysicsObject.PhysicsMode = false;
			trajectory = Trajectory.CreateTrajectory(location.Value);
		}
	}

	private void Start()
	{
		WorldTime.main.realtimePhysics.OnChange += new Action(UpdatePhysicsState);
		loader.onLoadedChange_Before += OnLoadedChange_Before;
		loader.onLoadedChange_After += OnLoadedChange_After;
	}

	private void Awake()
	{
		WorldView main = WorldView.main;
		main.onPositionOffset = (Action<Vector2>)Delegate.Combine(main.onPositionOffset, new Action<Vector2>(OnPositionOffset));
		WorldView main2 = WorldView.main;
		main2.onVelocityOffset = (Action<Vector2>)Delegate.Combine(main2.onVelocityOffset, new Action<Vector2>(OnVelocityOffset));
	}

	private void OnDestroy()
	{
		WorldTime.main.realtimePhysics.OnChange -= new Action(UpdatePhysicsState);
		loader.onLoadedChange_Before -= OnLoadedChange_Before;
		loader.onLoadedChange_After -= OnLoadedChange_After;
		WorldView main = WorldView.main;
		main.onPositionOffset = (Action<Vector2>)Delegate.Remove(main.onPositionOffset, new Action<Vector2>(OnPositionOffset));
		WorldView main2 = WorldView.main;
		main2.onVelocityOffset = (Action<Vector2>)Delegate.Remove(main2.onVelocityOffset, new Action<Vector2>(OnVelocityOffset));
	}

	private void UpdatePhysicsState()
	{
		PhysicsMode = WorldTime.main.realtimePhysics.Value && loader.Loaded;
	}

	private void OnLoadedChange_Before(bool _, bool newValue)
	{
		if (PhysicsMode)
		{
			PhysicsMode = WorldTime.main.realtimePhysics.Value & newValue;
		}
	}

	private void OnLoadedChange_After(bool _, bool newValue)
	{
		if (!PhysicsMode)
		{
			PhysicsMode = WorldTime.main.realtimePhysics.Value & newValue;
		}
	}

	private void OnPositionOffset(Vector2 offset)
	{
		if (PhysicsObject.PhysicsMode)
		{
			PhysicsObject.LocalPosition += offset;
		}
		else
		{
			PhysicsObject.LocalPosition = WorldView.ToLocalPosition(location.position);
		}
	}

	private void OnVelocityOffset(Vector2 offset)
	{
		if (PhysicsObject.PhysicsMode)
		{
			PhysicsObject.LocalVelocity += offset;
		}
	}

	private void FixedUpdate()
	{
		if (PhysicsMode)
		{
			location.position.Value = WorldView.ToGlobalPosition(PhysicsObject.LocalPosition);
			location.velocity.Value = WorldView.ToGlobalVelocity(PhysicsObject.LocalVelocity);
			location.planet.Value = WorldView.main.ViewLocation.planet;
			Vector2 gravity = Vector2.zero;
			if (!SandboxSettings.main.settings.noGravity || InOrbit())
			{
				gravity = (Vector2)location.planet.Value.GetGravity(WorldView.ToGlobalPosition(PhysicsObject.LocalPosition)) * Time.fixedDeltaTime;
			}
			PhysicsObject.OnFixedUpdate(gravity);
		}
	}

	private bool InOrbit()
	{
		Orbit orbit = Orbit.TryCreateOrbit(location.Value, calculateTimeParameters: false, calculateEncounters: false, out var success);
		if (success)
		{
			return orbit.periapsis > orbit.Planet.OrbitRadius;
		}
		return false;
	}

	private void Update()
	{
		if (PhysicsObject.PhysicsMode)
		{
			location.position.Value = WorldView.ToGlobalPosition(PhysicsObject.LocalPosition);
			location.velocity.Value = WorldView.ToGlobalVelocity(PhysicsObject.LocalVelocity);
			location.planet.Value = WorldView.main.ViewLocation.planet;
			if (WorldView.main.ViewLocation.planet.IsOutsideSOI(location.position) || location.planet.Value.satellites.Any((Planet satellite) => satellite.IsInsideSOI(location.position)))
			{
				PhysicsMode = false;
				trajectory.EnterNextPath();
				Update();
			}
			return;
		}
		trajectory.CheckEncounters();
		trajectory.CheckPathTransition(WorldTime.main.worldTime);
		location.Value = trajectory.GetLocation(WorldTime.main.worldTime);
		if (!PhysicsObject.PhysicsMode)
		{
			if (loader.Loaded)
			{
				PhysicsObject.LocalPosition = WorldView.ToLocalPosition(location.position);
			}
			if (location.velocity.Value.Mag_MoreThan(0.0) && location.planet.Value.IsInsideTerrain(location.position, 0.5))
			{
				PhysicsObject.OnCrashIntoPlanet();
			}
		}
	}

	public Trajectory GetTrajectory()
	{
		if (PhysicsObject.PhysicsMode)
		{
			Location value = location.Value;
			value.position -= PhysicsObject.LocalVelocity * Time.deltaTime;
			return Trajectory.CreateTrajectory(value);
		}
		return trajectory;
	}
}
