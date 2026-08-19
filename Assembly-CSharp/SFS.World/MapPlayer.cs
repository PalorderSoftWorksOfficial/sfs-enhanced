using System;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public abstract class MapPlayer : SelectableObject
{
	public MapIcon mapIcon;

	public override Location Location => Player.location.Value;

	public override int OrbitDepth => Player.location.planet.Value.orbitalDepth + 1;

	public override int ClickDepth => Player.location.planet.Value.orbitalDepth + 1;

	public abstract Player Player { get; }

	public abstract override Trajectory Trajectory { get; }

	private void Start()
	{
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
	}

	private new void OnDestroy()
	{
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
		base.OnDestroy();
	}

	private void OnPlayerChange()
	{
		if (mapIcon.mapIcon != null)
		{
			mapIcon.mapIcon.transform.localScale = Vector3.one * ((PlayerController.main.player.Value != null && this == PlayerController.main.player.Value.mapPlayer) ? 0.016f : 0.011f);
		}
	}

	public override bool Focus_FocusConditions(Double2 relativePosition, double viewDistance)
	{
		return viewDistance > relativePosition.magnitude;
	}

	public virtual void OnEndMission()
	{
		EndMissionMenu.main.OpenEndMissionMenu(Player);
	}
}
