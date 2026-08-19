using System.Linq;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class MapPlanet : SelectableObject
{
	public Planet planet;

	public override Location Location => planet.GetLocation(WorldTime.main.worldTime);

	public override Trajectory Trajectory => planet.trajectory;

	public override string EncounterText => Loc.main.Encounter;

	public override int OrbitDepth => planet.orbitalDepth;

	public override int ClickDepth => planet.orbitalDepth;

	public override Vector3 Select_MenuPosition => planet.mapHolder.transform.position;

	public double FocusDistance
	{
		get
		{
			if (!double.IsInfinity(planet.SOI))
			{
				return planet.SOI * 5.0;
			}
			return planet.satellites.Select((Planet satellite) => satellite.orbit.apoapsis).Max() * 5.0;
		}
	}

	public override string Select_DisplayName
	{
		get
		{
			return planet.DisplayName;
		}
		set
		{
		}
	}

	public override bool Select_CanNavigate => true;

	public override bool Select_CanFocus => true;

	public override bool Select_CanRename => false;

	public override bool Select_CanEndMission => false;

	public override string Select_EndMissionText => null;

	public override Sprite Select_EndMissionIcon => null;

	public override double Navigation_Tolerance => planet.SOI * 0.25;

	public override bool Focus_FocusConditions(Double2 relativePosition, double viewDistance)
	{
		if (viewDistance < planet.SOI * 5.0)
		{
			return relativePosition.Mag_LessThan(planet.SOI * 2.0);
		}
		return false;
	}

	public override bool IsViewTarget()
	{
		if (this == Map.view.view.target.Value)
		{
			return planet.SOI != double.PositiveInfinity;
		}
		return false;
	}
}
