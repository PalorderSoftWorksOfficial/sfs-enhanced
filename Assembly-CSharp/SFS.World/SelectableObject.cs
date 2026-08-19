using System.Collections.Generic;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public abstract class SelectableObject : ObservableMonoBehaviour
{
	public static List<SelectableObject> mapObjects = new List<SelectableObject>();

	public abstract Location Location { get; }

	public abstract Trajectory Trajectory { get; }

	public abstract string EncounterText { get; }

	public abstract int OrbitDepth { get; }

	public abstract int ClickDepth { get; }

	public abstract Vector3 Select_MenuPosition { get; }

	public abstract string Select_DisplayName { get; set; }

	public abstract bool Select_CanNavigate { get; }

	public abstract bool Select_CanFocus { get; }

	public abstract bool Select_CanRename { get; }

	public abstract bool Select_CanEndMission { get; }

	public abstract string Select_EndMissionText { get; }

	public abstract Sprite Select_EndMissionIcon { get; }

	public abstract double Navigation_Tolerance { get; }

	private void OnEnable()
	{
		mapObjects.Add(this);
	}

	private void OnDisable()
	{
		mapObjects.Remove(this);
	}

	public abstract bool Focus_FocusConditions(Double2 relativePosition, double viewDistance);

	public virtual bool IsViewTarget()
	{
		return this == Map.view.view.target.Value;
	}

	public bool IsNavigationTarget()
	{
		return this == Map.navigation.target;
	}
}
