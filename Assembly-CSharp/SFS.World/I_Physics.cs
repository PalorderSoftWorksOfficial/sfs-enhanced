using UnityEngine;

namespace SFS.World;

public interface I_Physics
{
	bool PhysicsMode { get; set; }

	Vector2 LocalPosition { get; set; }

	Vector2 LocalVelocity { get; set; }

	void OnFixedUpdate(Vector2 gravity);

	void OnCrashIntoPlanet();
}
