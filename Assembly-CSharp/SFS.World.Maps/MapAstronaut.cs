using SFS.Translations;
using UnityEngine;

namespace SFS.World.Maps;

public class MapAstronaut : MapPlayer
{
	public Astronaut_EVA astronaut;

	public override Player Player => astronaut;

	public override Trajectory Trajectory => astronaut.physics.GetTrajectory();

	public override string EncounterText => Loc.main.Rendezvous;

	public override Vector3 Select_MenuPosition
	{
		get
		{
			if (Map.manager.mapMode.Value)
			{
				return mapIcon.mapIcon.transform.position;
			}
			return astronaut.rb2d.worldCenterOfMass;
		}
	}

	public override string Select_DisplayName
	{
		get
		{
			return astronaut.astronaut.astronautName;
		}
		set
		{
		}
	}

	public override bool Select_CanNavigate => !Player.isPlayer.Value;

	public override bool Select_CanFocus => true;

	public override bool Select_CanRename => false;

	public override bool Select_CanEndMission => true;

	public override string Select_EndMissionText => Loc.main.Recover_Rocket;

	public override Sprite Select_EndMissionIcon => ResourcesLoader.main.buttonIcons.recover;

	public override double Navigation_Tolerance => 2500.0;

	public static bool CanRecover(Astronaut_EVA astronaut)
	{
		Location value = astronaut.location.Value;
		if (value.planet == Base.planetLoader.spaceCenter.Planet)
		{
			return value.velocity.Mag_LessThan(0.009999999776482582);
		}
		return false;
	}
}
