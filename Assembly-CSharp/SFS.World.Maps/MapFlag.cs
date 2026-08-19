using SFS.Input;
using SFS.Translations;
using SFS.UI;
using UnityEngine;

namespace SFS.World.Maps;

public class MapFlag : MapPlayer
{
	public Flag flag;

	public override Player Player => flag;

	public override Trajectory Trajectory { get; } = Trajectory.Empty;

	public override string EncounterText => null;

	public override Vector3 Select_MenuPosition
	{
		get
		{
			if (Map.manager.mapMode.Value)
			{
				return mapIcon.mapIcon.transform.position + new Vector3(0.0045f, 0.007f) * (float)((double)Map.view.view.distance / 1000.0);
			}
			return flag.holder.position;
		}
	}

	public override string Select_DisplayName
	{
		get
		{
			return Loc.main.Flag;
		}
		set
		{
		}
	}

	public override bool Select_CanNavigate => false;

	public override bool Select_CanFocus => false;

	public override bool Select_CanRename => false;

	public override bool Select_CanEndMission => true;

	public override string Select_EndMissionText => Loc.main.Remove_Flag;

	public override Sprite Select_EndMissionIcon => ResourcesLoader.main.buttonIcons.removeFlag;

	public override double Navigation_Tolerance => 2500.0;

	public override void OnEndMission()
	{
		MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Confirm_Remove_Flag, () => Loc.main.Remove_Flag, delegate
		{
			AstronautManager.DestroyFlag(flag);
		});
	}
}
