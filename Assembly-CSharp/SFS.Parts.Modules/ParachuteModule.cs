using System;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;
using UnityEngine.Events;

namespace SFS.Parts.Modules;

public class ParachuteModule : MonoBehaviour, Rocket.INJ_Location, I_PartMenu
{
	public double maxDeployHeight;

	public double maxDeployVelocity;

	[Space]
	public AnimationCurve drag;

	public Transform parachute;

	[Space]
	public Float_Reference state;

	public Float_Reference targetState;

	private Double2 oldPosition;

	[Space]
	public UnityEvent onDeploy;

	public Location Location { private get; set; }

	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		drawer.DrawStat(40, Loc.main.Info_Parachute_Max_Height.Inject(maxDeployHeight.ToDistanceString(), "height"));
	}

	public void DeployParachute(UsePartData data)
	{
		bool flag = false;
		double num = (Location.planet.HasAtmospherePhysics ? Location.planet.data.atmospherePhysics.parachuteMultiplier : 1.0);
		if (targetState.Value == 0f && state.Value == 0f)
		{
			if (!Location.planet.HasAtmospherePhysics || Location.Height > Location.planet.AtmosphereHeightPhysics * 0.9)
			{
				MsgDrawer.main.Log(Loc.main.Msg_Cannot_Deploy_Parachute_In_Vacuum);
			}
			else if (Location.TerrainHeight > maxDeployHeight * num)
			{
				MsgDrawer.main.Log(Loc.main.Msg_Cannot_Deploy_Parachute_Above.Inject((maxDeployHeight * num).ToDistanceString(decimals: false), "height"));
			}
			else if (Location.velocity.magnitude > maxDeployVelocity * num)
			{
				MsgDrawer.main.Log(Loc.main.Msg_Cannot_Deploy_Parachute_While_Faster.Inject((maxDeployVelocity * num).ToVelocityString(decimals: false), "velocity"));
			}
			else if (Location.velocity.magnitude < 3.0)
			{
				MsgDrawer.main.Log(Loc.main.Msg_Cannot_Deploy_Parachute_While_Not_Moving);
			}
			else
			{
				MsgDrawer.main.Log(Loc.main.Msg_Parachute_Half_Deployed);
				targetState.Value = 1f;
				onDeploy.Invoke();
				flag = true;
			}
		}
		else if (targetState.Value == 1f && state.Value == 1f)
		{
			if (Location.TerrainHeight > maxDeployHeight * 0.2 * num)
			{
				MsgDrawer.main.Log(Loc.main.Msg_Cannot_Fully_Deploy_Above.Inject((maxDeployHeight * 0.2 * num).ToDistanceString(decimals: false), "height"));
			}
			else
			{
				MsgDrawer.main.Log(Loc.main.Msg_Parachute_Fully_Deployed);
				targetState.Value = 2f;
				flag = true;
			}
		}
		else if (targetState.Value == 2f && state.Value == 2f)
		{
			MsgDrawer.main.Log(Loc.main.Msg_Parachute_Cut);
			targetState.Value = 3f;
			state.Value = 3f;
			flag = true;
		}
		else if (targetState.Value == 3f)
		{
			flag = true;
		}
		if (!flag)
		{
			data.successfullyUsedPart = false;
		}
	}

	private void Start()
	{
		if (GameManager.main == null)
		{
			base.enabled = false;
		}
		else
		{
			targetState.OnChange += new Action(UpdateEnabled);
		}
	}

	private void UpdateEnabled()
	{
		base.enabled = targetState.Value == 1f || targetState.Value == 2f;
	}

	private void LateUpdate()
	{
		if (GameManager.main == null || Location.planet == null)
		{
			return;
		}
		if (oldPosition.x == 0.0 && oldPosition.y == 0.0)
		{
			oldPosition = WorldView.ToGlobalPosition(base.transform.position) - Location.velocity;
			AngleToOldPosition();
			return;
		}
		AngleToOldPosition();
		if (targetState.Value == 1f && Location.TerrainHeight < maxDeployHeight * 0.2 * Location.planet.data.atmospherePhysics.parachuteMultiplier - 250.0)
		{
			targetState.Value = 2f;
		}
		if (targetState.Value == 2f && Location.velocity.Mag_LessThan(0.009999999776482582))
		{
			targetState.Value = 3f;
			state.Value = 3f;
		}
	}

	private void AngleToOldPosition()
	{
		Double2 @double = WorldView.ToGlobalPosition(base.transform.position);
		Double2 double2 = oldPosition - @double;
		if (double2.Mag_MoreThan(10.0))
		{
			oldPosition = @double + double2.normalized * 10.0;
		}
		Vector2 vector = parachute.parent.InverseTransformVector(double2);
		float num = Mathf.Atan2(vector.y, vector.x) * 57.29578f - 90f;
		parachute.localEulerAngles = new Vector3(0f, 0f, num + Mathf.Sin(Time.time) * 3f * parachute.parent.lossyScale.x * parachute.parent.lossyScale.y);
	}
}
