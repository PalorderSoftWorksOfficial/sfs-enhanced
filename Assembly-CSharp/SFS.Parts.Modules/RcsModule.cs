using System;
using System.Collections.Generic;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class RcsModule : MonoBehaviour, Rocket.INJ_Rocket, Rocket.INJ_IsPlayer, Rocket.INJ_DirectionalAxis, Rocket.INJ_TurnAxisTorque, I_PartMenu
{
	[Serializable]
	public class Thruster
	{
		public Vector2 thrustNormal;

		public MoveModule effect;
	}

	public float directionAngleThreshold;

	public float torqueAngleThreshold;

	public float thrust;

	public float ISP;

	public List<Thruster> thrusters = new List<Thruster>();

	public FlowModule source;

	public Vector2 thrustPosition;

	public Rocket Rocket { get; set; }

	public bool IsPlayer { get; set; }

	public float TurnAxis { get; set; }

	public Vector2 DirectionalAxis { get; set; }

	private bool RCS_On
	{
		get
		{
			return Rocket.arrowkeys.rcs.Value;
		}
		set
		{
			Rocket.arrowkeys.rcs.Value = value;
		}
	}

	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		drawer.DrawStat(60, thrust.ToThrustString());
		drawer.DrawStat(50, ISP.ToEfficiencyString());
	}

	private void Start()
	{
		source.onStateChange += Update_RCS_On;
	}

	private void Update_RCS_On()
	{
		if (RCS_On)
		{
			I_MsgLogger i_MsgLogger2;
			if (!IsPlayer)
			{
				I_MsgLogger i_MsgLogger = new MsgNone();
				i_MsgLogger2 = i_MsgLogger;
			}
			else
			{
				I_MsgLogger i_MsgLogger = MsgDrawer.main;
				i_MsgLogger2 = i_MsgLogger;
			}
			I_MsgLogger logger = i_MsgLogger2;
			if (!source.CanFlow(logger))
			{
				ToggleRCS(new UsePartData(new UsePartData.SharedData(fromStaging: false), null), showMsg: false);
			}
		}
	}

	public void ToggleRCS(UsePartData data)
	{
		ToggleRCS(data, showMsg: true);
	}

	private void ToggleRCS(UsePartData data, bool showMsg)
	{
		if (data.sharedData.hasToggledRCS)
		{
			return;
		}
		data.sharedData.hasToggledRCS = true;
		I_MsgLogger i_MsgLogger2;
		if (!showMsg || !IsPlayer)
		{
			I_MsgLogger i_MsgLogger = new MsgNone();
			i_MsgLogger2 = i_MsgLogger;
		}
		else
		{
			I_MsgLogger i_MsgLogger = MsgDrawer.main;
			i_MsgLogger2 = i_MsgLogger;
		}
		I_MsgLogger i_MsgLogger3 = i_MsgLogger2;
		if (RCS_On || source.CanFlow(i_MsgLogger3))
		{
			RCS_On = !RCS_On;
			if (RCS_On)
			{
				i_MsgLogger3.Log(string.Concat(Loc.main.Msg_RCS_Module_State.InjectField(RCS_On.State_ToOnOff(), "state"), "\n\nUse ", KeybindingsPC.keys.Turn_Rocket.ToKeysString(), " to turn, ", KeybindingsPC.keys.Move_Rocket_Using_RCS.ToKeysString(), " to move"));
			}
			else
			{
				i_MsgLogger3.Log(Loc.main.Msg_RCS_Module_State.InjectField(RCS_On.State_ToOnOff(), "state"));
			}
		}
	}

	private void FixedUpdate()
	{
		if (Rocket == null || !RCS_On || (Mathf.Abs(TurnAxis) < 0.01f && (double)DirectionalAxis.sqrMagnitude < 0.01))
		{
			foreach (Thruster thruster in thrusters)
			{
				thruster.effect.targetTime.Value = 0f;
			}
			source.SetMassFlow(0.0);
			return;
		}
		Vector2 positionToCenterOfMass = Rocket.rb2d.worldCenterOfMass - (Vector2)base.transform.TransformPoint(thrustPosition);
		float num = 0f;
		Vector2 zero = Vector2.zero;
		foreach (Thruster thruster2 in thrusters)
		{
			Vector2 vector = base.transform.TransformVectorUnscaled(thruster2.thrustNormal);
			bool num2 = TorqueThrust(vector, positionToCenterOfMass);
			bool flag = DirectionThrust(vector);
			if (num2 | flag)
			{
				zero += vector;
				num++;
				thruster2.effect.targetTime.Value = 1f;
			}
			else
			{
				thruster2.effect.targetTime.Value = 0f;
			}
		}
		if (zero != Vector2.zero)
		{
			Rocket.rb2d.AddForceAtPosition(zero * (thrust * num * 9.8f), base.transform.TransformPoint(thrustPosition));
		}
		source.SetMassFlow(thrust * num / ISP);
	}

	private bool TorqueThrust(Vector2 thrustDirection, Vector2 positionToCenterOfMass)
	{
		if (thrustDirection == Vector2.zero || positionToCenterOfMass == Vector2.zero)
		{
			return false;
		}
		if (Mathf.Abs(TurnAxis) < 0.95f && Mathf.Abs(Rocket.rb2d.angularVelocity) < 2f)
		{
			return false;
		}
		float num = Vector2.Angle(thrustDirection, Quaternion.Euler(0f, 0f, 90f) * positionToCenterOfMass);
		float num2 = Vector2.Angle(thrustDirection, Quaternion.Euler(0f, 0f, -90f) * positionToCenterOfMass);
		if (!(TurnAxis > 0.1f) || !(num <= torqueAngleThreshold))
		{
			if (TurnAxis < -0.1f)
			{
				return num2 <= torqueAngleThreshold;
			}
			return false;
		}
		return true;
	}

	private bool DirectionThrust(Vector2 thrustDirection)
	{
		if (DirectionalAxis == Vector2.zero)
		{
			return false;
		}
		return Vector2.Angle(thrustDirection, DirectionalAxis) <= directionAngleThreshold;
	}
}
