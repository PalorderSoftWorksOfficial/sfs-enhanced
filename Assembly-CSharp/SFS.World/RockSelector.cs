using System;
using System.Collections.Generic;
using SFS.Career;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class RockSelector : SelectableObject
{
	public static RockSelector main;

	public HashSet<Transform> rockInstances = new HashSet<Transform>();

	private Transform selectedRock;

	public override Location Location => new Location(WorldView.main.ViewLocation.planet, WorldView.ToGlobalPosition(selectedRock.position));

	public override Trajectory Trajectory => null;

	public override string EncounterText => null;

	public override int OrbitDepth => -1;

	public override int ClickDepth => -1;

	public override Vector3 Select_MenuPosition => selectedRock.position + WorldView.ToGlobalPosition(selectedRock.position).normalized * selectedRock.transform.lossyScale.y * 0.20000000298023224;

	public override string Select_DisplayName
	{
		get
		{
			return Loc.main.Planets_Rock.InjectField(WorldView.main.ViewLocation.planet.DisplayName, "planet");
		}
		set
		{
		}
	}

	public override bool Select_CanNavigate => false;

	public override bool Select_CanFocus => false;

	public override bool Select_CanRename => false;

	public override bool Select_CanEndMission => true;

	public override string Select_EndMissionText => Loc.main.Collect_Rock;

	public override Sprite Select_EndMissionIcon => ResourcesLoader.main.buttonIcons.collectRock;

	public override double Navigation_Tolerance => 0.0;

	private void Awake()
	{
		main = this;
	}

	public bool TrySelect(Vector2 clickPosition)
	{
		float num = 0.7f;
		Transform transform = null;
		foreach (Transform rockInstance in rockInstances)
		{
			if (!(rockInstance == null) && !(rockInstance.lossyScale.x * rockInstance.lossyScale.y > 0.122499995f))
			{
				float sqrMagnitude = ((Vector2)rockInstance.position - clickPosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					transform = rockInstance;
					num = sqrMagnitude;
				}
			}
		}
		if (transform != null)
		{
			Select(transform);
			return true;
		}
		return false;
	}

	private void Select(Transform a)
	{
		selectedRock = a;
		GameSelector.main.selected_World.Value = this;
	}

	public void CollectRock()
	{
		if (PlayerController.main.player.Value is Astronaut_EVA astronaut_EVA)
		{
			Location location = Location;
			astronaut_EVA.stats.OnCollectRock(location.position.AngleDegrees);
			rockInstances.Remove(selectedRock);
			UnityEngine.Object.Destroy(selectedRock.gameObject);
			GameSelector.main.selected_World.Value = null;
			Dictionary<string, HashSet<long>> collectedRocks = AstronautState.main.state.collectedRocks;
			if (!collectedRocks.ContainsKey(location.planet.codeName))
			{
				collectedRocks.Add(location.planet.codeName, new HashSet<long>());
			}
			collectedRocks[location.planet.codeName].Add(GetRockID(location.position.AngleRadians, location.planet));
		}
	}

	public static long GetRockID(double angleRadians, Planet planet)
	{
		return (long)((angleRadians / (Math.PI * 2.0) + 10.0) % 1.0 * planet.SurfaceArea * 100.0);
	}

	public override bool Focus_FocusConditions(Double2 a, double b)
	{
		return false;
	}
}
