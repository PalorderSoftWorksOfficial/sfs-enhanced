using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.World.Drag;

public class Aero_Astronaut : AeroModule
{
	[Space]
	public Astronaut_EVA astronaut;

	protected override bool PhysicsMode => astronaut.physics.PhysicsMode;

	private void OnCollisionStay2D()
	{
		if (!astronaut.CanPickItselfUp)
		{
			astronaut.ragdollTime = 0f;
		}
	}

	protected override Location GetLocation()
	{
		return astronaut.location.Value;
	}

	protected override List<Surface> GetDragSurfaces(Matrix2x2 rotate)
	{
		Vector2[] array = new Vector2[4]
		{
			new Vector2(-0.45f, 0f),
			new Vector2(-0.45f, 1.8f),
			new Vector2(0.45f, 1.8f),
			new Vector2(0.45f, 0f)
		}.Select((Vector2 p) => (Vector2)astronaut.rb2d.transform.TransformPoint(p) * rotate).ToArray();
		List<Surface> output = new List<Surface>();
		for (int num = 0; num < array.Length - 1; num++)
		{
			AddLine(array[num], array[num + 1]);
		}
		AddLine(array[array.Length - 1], array[0]);
		return output;
		void AddLine(Vector2 a, Vector2 b)
		{
			if (a.x < b.x)
			{
				output.Add(new Surface(astronaut.resources, astronaut.resources.valid, new Line2(a, b)));
			}
		}
	}

	protected override void AddForceAtPosition(Vector2 force, Vector2 position)
	{
		astronaut.rb2d.velocity += force * (0.7f * Time.fixedDeltaTime);
	}

	protected override float GetMass()
	{
		return 1f;
	}
}
