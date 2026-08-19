using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class WorldLocation : MonoBehaviour
{
	public Planet_Local planet = new Planet_Local();

	public Double2_Local position = new Double2_Local();

	public Double2_Local velocity = new Double2_Local();

	public Location Value
	{
		get
		{
			return new Location((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0, planet, position, velocity);
		}
		set
		{
			if (value != null && !(value.planet == null))
			{
				planet.Value = value.planet;
				position.Value = value.position;
				velocity.Value = value.velocity;
			}
		}
	}
}
