using System.Collections.Generic;
using SFS.Career;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using UnityEngine;

namespace SFS.World;

public class AstronautManager : MonoBehaviour
{
	public static AstronautManager main;

	public Astronaut_EVA astronautPrefab;

	public Flag flagPrefab;

	public MoveModule fadeToBlack;

	public List<Astronaut_EVA> eva = new List<Astronaut_EVA>();

	public List<Flag> flags = new List<Flag>();

	private void Awake()
	{
		main = this;
	}

	public Astronaut_EVA SpawnEVA(string astronautName, Location location, float rotation, float angularVelocity, bool ragdoll, double fuelPercent, float temperature)
	{
		Astronaut_EVA astronaut_EVA = Object.Instantiate(astronautPrefab);
		astronaut_EVA.astronaut = AstronautState.main.GetAstronautByName(astronautName);
		astronaut_EVA.physics = astronaut_EVA.gameObject.GetComponentInChildren<Physics>();
		astronaut_EVA.physics.SetLocationAndState(location, physicsMode: false);
		astronaut_EVA.rb2d.rotation = rotation;
		astronaut_EVA.rb2d.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
		astronaut_EVA.rb2d.angularVelocity = angularVelocity;
		astronaut_EVA.SetRagdoll(ragdoll);
		astronaut_EVA.resources.fuelPercent.Value = fuelPercent;
		astronaut_EVA.resources.temperature.Value = temperature;
		return astronaut_EVA;
	}

	public static void DestroyEVA(Astronaut_EVA astronaut, bool death)
	{
		if (death)
		{
			AstronautState.main.GetAstronautByName(astronaut.astronaut.astronautName).alive = false;
		}
		Object.Destroy(astronaut.gameObject);
	}

	public void PlantFlag()
	{
		if (!(PlayerController.main.player.Value is Astronaut_EVA { location: var location } astronaut_EVA))
		{
			return;
		}
		double angleRadians = location.position.Value.AngleRadians - (double)((float)astronaut_EVA.facingDirection * 1.1f) / location.position.Value.magnitude;
		Location location2 = new Location(WorldTime.main.worldTime, location.planet.Value, Double2.CosSin(angleRadians, location.planet.Value.Radius), Double2.zero);
		Astronaut_EVA.GetGroundRadius(location2, out var _, out var groundRadius);
		location2.position = Double2.CosSin(angleRadians, groundRadius);
		if ((location.position.Value - location2.position).Mag_MoreThan(5.0))
		{
			MsgDrawer.main.Log(Loc.main.Cannot_Plant_Flag_Here);
			return;
		}
		foreach (Flag flag in flags)
		{
			if (flag.location.planet.Value == location.planet.Value && (flag.location.position.Value - location.position.Value).Mag_LessThan(30.0))
			{
				MsgDrawer.main.Log(Loc.main.Cannot_Plant_Flag_Near_Another_Flag);
				return;
			}
		}
		SpawnFlag(location2, astronaut_EVA.facingDirection).ShowPlantAnimation();
		astronaut_EVA.stats.OnPlantFlag(location2.position.AngleDegrees);
	}

	public Flag SpawnFlag(Location location, int direction)
	{
		Flag flag = Object.Instantiate(flagPrefab);
		flag.location.Value = location;
		flag.direction = direction;
		return flag;
	}

	public static void DestroyFlag(Flag flag)
	{
		Object.Destroy(flag.gameObject);
	}
}
