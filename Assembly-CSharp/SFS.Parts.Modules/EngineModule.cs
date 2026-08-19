using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class EngineModule : MonoBehaviour, Rocket.INJ_IsPlayer, Rocket.INJ_Physics, Rocket.INJ_Throttle, Rocket.INJ_TurnAxisTorque
{
	public Composed_Float thrust;

	public Composed_Vector2 thrustNormal = new Composed_Vector2(Vector2.up);

	public Composed_Float ISP;

	public Composed_Vector2 thrustPosition = new Composed_Vector2(Vector2.zero);

	public FlowModule source;

	public bool hasGimbal = true;

	public Bool_Reference gimbalOn;

	public MoveModule gimbal;

	public Bool_Reference engineOn;

	public Float_Reference throttle_Out;

	public Bool_Reference heatOn;

	public GameObject heatHolder;

	private Vector3 originalPosition;

	private readonly Float_Local throttle_Input = new Float_Local();

	private readonly Float_Local turnAxis_Input = new Float_Local();

	[Space]
	public float oldMass = float.NaN;

	public bool IsPlayer { get; set; }

	public Rigidbody2D Rb2d { get; set; }

	float Rocket.INJ_Throttle.Throttle
	{
		set
		{
			throttle_Input.Value = value;
		}
	}

	float Rocket.INJ_TurnAxisTorque.TurnAxis
	{
		set
		{
			turnAxis_Input.Value = value;
		}
	}

	private I_MsgLogger Logger
	{
		get
		{
			if (!IsPlayer)
			{
				return new MsgNone();
			}
			return MsgDrawer.main;
		}
	}

	private bool HasFuel(I_MsgLogger logger)
	{
		return source.CanFlow(logger);
	}

	public void Draw(List<EngineModule> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		drawer.DrawStat(50, thrust.Value.ToThrustString());
		drawer.DrawStat(40, (ISP.Value * (float)Base.worldBase.settings.difficulty.IspMultiplier).ToEfficiencyString());
		if (!settings.build && !settings.game)
		{
			return;
		}
		drawer.DrawToggle(0, () => Loc.main.Engine_On_Label, ToggleEngineOn, () => engineOn.Value, delegate(Action update)
		{
			engineOn.OnChange += update;
		}, delegate(Action update)
		{
			engineOn.OnChange -= update;
		});
		if (hasGimbal)
		{
			drawer.DrawToggle(0, () => Loc.main.Gimbal_On_Label, ToggleGimbal, () => gimbalOn.Value, delegate(Action update)
			{
				gimbalOn.OnChange += update;
			}, delegate(Action update)
			{
				gimbalOn.OnChange -= update;
			});
		}
		void ToggleEngineOn()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				bool value = !engineOn.Value;
				foreach (EngineModule module in modules)
				{
					module.engineOn.Value = value;
				}
			});
		}
		void ToggleGimbal()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				bool value = !gimbalOn.Value;
				foreach (EngineModule module2 in modules)
				{
					if (hasGimbal)
					{
						module2.gimbalOn.Value = value;
					}
				}
			});
		}
	}

	private void Start()
	{
		if (HomeManager.main != null)
		{
			base.enabled = false;
			return;
		}
		source.onStateChange += CheckOutOfFuel;
		thrust.OnChange += new Action(RecalculateMassFlow);
		ISP.OnChange += new Action(RecalculateMassFlow);
		throttle_Out.OnChange += new Action(RecalculateMassFlow);
		throttle_Out.OnChange += new Action(UpdateApplyPhysics);
		if (GameManager.main != null)
		{
			CheckOutOfFuel();
		}
		engineOn.OnChange += new Action(RecalculateEngineThrottle);
		throttle_Input.OnChange += new Action(RecalculateEngineThrottle);
		if (hasGimbal)
		{
			throttle_Out.OnChange += new Action(RecalculateGimbal);
			turnAxis_Input.OnChange += new Action(RecalculateGimbal);
		}
		heatHolder.gameObject.SetActive(heatOn.Value || !Base.worldBase.AllowsCheats);
		if (GameManager.main != null)
		{
			originalPosition = heatHolder.transform.localPosition;
			WorldView main = WorldView.main;
			main.onVelocityOffset = (Action<Vector2>)Delegate.Combine(main.onVelocityOffset, new Action<Vector2>(PositionFlameHitbox));
		}
	}

	private void OnDestroy()
	{
		if (GameManager.main != null)
		{
			WorldView main = WorldView.main;
			main.onVelocityOffset = (Action<Vector2>)Delegate.Remove(main.onVelocityOffset, new Action<Vector2>(PositionFlameHitbox));
		}
	}

	private void PositionFlameHitbox(Vector2 _)
	{
		PositionFlameHitbox();
	}

	private void RecalculateMassFlow()
	{
		float magnitude = base.transform.TransformVector(thrustNormal.Value).magnitude;
		source.SetMassFlow(thrust.Value * magnitude * throttle_Out.Value / (ISP.Value * (float)Base.worldBase.settings.difficulty.IspMultiplier));
	}

	private void CheckOutOfFuel()
	{
		if (engineOn.Value && !HasFuel(Logger))
		{
			engineOn.Value = false;
		}
	}

	private void RecalculateEngineThrottle()
	{
		throttle_Out.Value = (engineOn.Value ? throttle_Input.Value : 0f);
	}

	private void RecalculateGimbal()
	{
		if (hasGimbal && gimbalOn.Value)
		{
			gimbal.targetTime.Value = ((throttle_Out.Value > 0f) ? (turnAxis_Input.Value * base.transform.RotationDirection()) : 0f);
		}
	}

	private void UpdateApplyPhysics()
	{
		base.enabled = Rb2d != null && throttle_Out.Value > 0f;
	}

	private void FixedUpdate()
	{
		if (!(Rb2d == null))
		{
			Vector2 vector = thrustNormal.Value * (thrust.Value * 9.8f * throttle_Out.Value);
			Vector2 force = (Base.worldBase.AllowsCheats ? ((Vector2)base.transform.TransformVector(vector)) : base.transform.TransformVectorUnscaled(vector));
			Vector2 relativePoint = Rb2d.GetRelativePoint(Transform_Utility.LocalToLocalPoint(base.transform, Rb2d, thrustPosition.Value));
			Rb2d.AddForceAtPosition(force, relativePoint, ForceMode2D.Force);
			PositionFlameHitbox();
		}
	}

	private void PositionFlameHitbox()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			heatHolder.transform.localPosition = originalPosition + heatHolder.transform.parent.InverseTransformVector(Rb2d.velocity * Time.fixedDeltaTime);
		}
	}

	public void ToggleEngine()
	{
		if (engineOn.Value)
		{
			DisableEngine(Logger);
		}
		else
		{
			EnableEngine(Logger);
		}
	}

	private void EnableEngine(I_MsgLogger logger)
	{
		if (HasFuel(logger))
		{
			engineOn.Value = true;
			if (throttle_Out.Value == 0f)
			{
				logger.Log(Loc.main.Engine_Module_State.InjectField(engineOn.Value.State_ToOnOff(), "state"));
			}
		}
	}

	private void DisableEngine(I_MsgLogger logger)
	{
		bool num = throttle_Out.Value == 0f;
		engineOn.Value = false;
		if (num)
		{
			logger.Log(Loc.main.Engine_Module_State.InjectField(engineOn.Value.State_ToOnOff(), "state"));
		}
	}

	private void Awake()
	{
		if (!float.IsNaN(oldMass))
		{
			double num = (Base.worldBase.insideWorld.Value ? Base.worldBase.settings.difficulty.EngineMassMultiplier : 1.0);
			GetComponent<VariablesModule>().doubleVariables.SetValue("mass", (double)oldMass * num, (true, false));
		}
	}
}
