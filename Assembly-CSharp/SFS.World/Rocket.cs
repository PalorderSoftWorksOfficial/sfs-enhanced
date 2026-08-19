using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Stats;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World.Drag;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class Rocket : Player, I_Physics
{
	public interface INJ_Rocket
	{
		Rocket Rocket { set; }
	}

	public interface INJ_IsPlayer
	{
		bool IsPlayer { set; }
	}

	public interface INJ_HasControl
	{
		bool HasControl { set; }
	}

	public interface INJ_ThrottleOn
	{
		bool ThrottleOn { set; }
	}

	public interface INJ_Throttle
	{
		float Throttle { set; }
	}

	public interface INJ_TurnAxisTorque
	{
		float TurnAxis { set; }
	}

	public interface INJ_TurnAxisWheels
	{
		float TurnAxis { set; }
	}

	public interface INJ_DirectionalAxis
	{
		Vector2 DirectionalAxis { set; }
	}

	public interface INJ_Physics
	{
		Rigidbody2D Rb2d { set; }
	}

	public interface INJ_Location
	{
		Location Location { set; }
	}

	public Mass_Calculator mass;

	public Rigidbody2D rb2d;

	public PartHolder partHolder;

	public MapIcon mapIcon;

	public Arrowkeys arrowkeys;

	public Throttle throttle;

	public Staging staging;

	public Resources resources;

	public Aero_Rocket aero;

	public StatsRecorder stats;

	public GameObject timeManager;

	public GameObject partManager;

	public string rocketName;

	public JointGroup jointsGroup;

	public float collisionImmunity;

	private List<(ResourceModule[], ResourceModule)> pipeFlows = new List<(ResourceModule[], ResourceModule)>();

	private float sizeRadius;

	private float lastUpdateTime = -10f;

	public Float_Local output_TurnAxisTorque;

	public Float_Local output_TurnAxisWheels;

	public Vector2_Local output_DirectionalAxis;

	public Physics physics;

	bool I_Physics.PhysicsMode
	{
		get
		{
			if (rb2d != null)
			{
				return rb2d.simulated;
			}
			return false;
		}
		set
		{
			rb2d.simulated = value;
			Collider2D[] componentsInChildren = partHolder.GetComponentsInChildren<Collider2D>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = value;
			}
			if (!rb2d.simulated)
			{
				rb2d.angularVelocity = 0f;
			}
		}
	}

	Vector2 I_Physics.LocalPosition
	{
		get
		{
			return (Vector2)rb2d.transform.position + (Vector2)rb2d.transform.TransformVector(mass.GetCenterOfMass());
		}
		set
		{
			Transform obj = rb2d.transform;
			Vector2 vector = (rb2d.position = value - (Vector2)rb2d.transform.TransformVector(mass.GetCenterOfMass()));
			obj.position = vector;
		}
	}

	Vector2 I_Physics.LocalVelocity
	{
		get
		{
			return rb2d.velocity;
		}
		set
		{
			rb2d.velocity = value;
		}
	}

	private bool IsOnSurface
	{
		get
		{
			Collider2D[] array = new Collider2D[5];
			rb2d.GetContacts(array);
			return array.Any((Collider2D a) => a != null && a.gameObject.layer == LayerMask.NameToLayer("Celestial Body"));
		}
	}

	private void Awake()
	{
		Start_RocketInjector();
		partHolder.TrackModules(delegate(ControlModule addedModule)
		{
			addedModule.hasControl.OnChange += new Action(UpdateControl);
		}, delegate(ControlModule removedModule)
		{
			removedModule.hasControl.OnChange -= new Action(UpdateControl);
		}, UpdateControl);
		void UpdateControl()
		{
			hasControl.Value = partHolder.GetModules<ControlModule>().Any((ControlModule module) => module.hasControl.Value);
		}
	}

	private new void OnDestroy()
	{
		OnDestroy_RocketInjector();
		base.OnDestroy();
	}

	private void OnEnable()
	{
		GameManager.main.rockets.Add(this);
	}

	private void OnDisable()
	{
		GameManager.main.rockets.Remove(this);
	}

	public void EnableCollisionImmunity(float duration)
	{
		collisionImmunity = Time.time + duration;
	}

	private void UpdateMass()
	{
		rb2d.mass = mass.GetMass();
		rb2d.centerOfMass = mass.GetCenterOfMass();
	}

	private float GetTorque()
	{
		float num = 0f;
		TorqueModule[] modules = partHolder.GetModules<TorqueModule>();
		foreach (TorqueModule torqueModule in modules)
		{
			if (torqueModule.enabled.Local || torqueModule.enabled.Value)
			{
				num += torqueModule.torque.Value;
			}
		}
		return num;
	}

	private void UpdateMapIconRotation()
	{
		mapIcon.SetRotation(GetRotation());
	}

	public float GetRotation()
	{
		Vector3 zero = Vector3.zero;
		Vector3 zero2 = Vector3.zero;
		EngineModule[] modules = partHolder.GetModules<EngineModule>();
		foreach (EngineModule engineModule in modules)
		{
			if (engineModule.engineOn.Value)
			{
				zero += engineModule.transform.TransformVector(engineModule.thrustNormal.Value * engineModule.thrust.Value);
			}
			else
			{
				zero2 += engineModule.transform.TransformVector(engineModule.thrustNormal.Value * engineModule.thrust.Value);
			}
		}
		if (zero != Vector3.zero)
		{
			return Mathf.Atan2(zero.y, zero.x) * 57.29578f;
		}
		if (zero2 != Vector3.zero)
		{
			return Mathf.Atan2(zero2.y, zero2.x) * 57.29578f;
		}
		ControlModule[] modules2 = partHolder.GetModules<ControlModule>();
		int i = 0;
		if (i < modules2.Length)
		{
			return modules2[i].transform.eulerAngles.z + 90f;
		}
		return base.transform.eulerAngles.z;
	}

	public void SetJointGroup(JointGroup jointsGroup)
	{
		this.jointsGroup = jointsGroup;
		if (jointsGroup.parts.Count == 0)
		{
			RocketManager.DestroyRocket(this, DestructionReason.Intentional);
			hasControl.Value = false;
			partHolder.SetParts();
		}
		else
		{
			SetParts(jointsGroup.parts.ToArray());
		}
	}

	public void SetParts(Part[] newParts)
	{
		partHolder.SetParts(newParts);
		for (int i = 0; i < newParts.Length; i++)
		{
			newParts[i].transform.parent = partHolder.transform;
		}
		jointsGroup.RepositionParts();
		InjectPartDependencies();
		UpdateMass();
		if (arrowkeys.rcs.Value && !partHolder.HasModule<RcsModule>())
		{
			arrowkeys.rcs.Value = false;
		}
		resources.SetupResourceGroups(this);
		ResourceModule[] modules = partHolder.GetModules<ResourceModule>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].flowModules.Clear();
		}
		FuelPipeModule[] modules2 = partHolder.GetModules<FuelPipeModule>();
		FuelPipeModule[] array = modules2;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].FindNeighbours(jointsGroup);
		}
		pipeFlows.Clear();
		array = modules2;
		for (int i = 0; i < array.Length; i++)
		{
			(ResourceModule[], ResourceModule)? tuple = array[i].FindFlow();
			if (tuple.HasValue)
			{
				pipeFlows.Add(tuple.Value);
			}
		}
		FlowModule[] modules3 = partHolder.GetModules<FlowModule>();
		for (int i = 0; i < modules3.Length; i++)
		{
			modules3[i].FindSources(this);
		}
		resources.RemoveInvalidTransfers(partHolder);
		DockingPortModule[] modules4 = partHolder.GetModules<DockingPortModule>();
		for (int i = 0; i < modules4.Length; i++)
		{
			modules4[i].UpdateOccupied();
		}
		aero.heatManager.OnSetParts(newParts);
		aero.burnManager.ApplyOpacity();
	}

	public override float GetSizeRadius()
	{
		if (Time.time - lastUpdateTime < 1f)
		{
			return sizeRadius;
		}
		Vector2 centerOfMass = rb2d.centerOfMass;
		float f = partHolder.parts.Max((Part part) => ((Vector2)part.transform.localPosition + part.centerOfMass.Value * part.orientation.orientation.Value - centerOfMass).sqrMagnitude);
		sizeRadius = Mathf.Sqrt(f) + 3f;
		lastUpdateTime = Time.time;
		return sizeRadius;
	}

	public override void ClampTrackingOffset(ref Vector2 trackingOffset, float cameraDistance)
	{
		float num = GetSizeRadius();
		float num2 = cameraDistance * 0.4f;
		float num3 = num - num2;
		if (num3 <= 0f)
		{
			trackingOffset = Vector2.zero;
		}
		else if (trackingOffset.sqrMagnitude > Mathf.Pow(num3, 2f))
		{
			trackingOffset = trackingOffset.normalized * num3;
		}
	}

	public override bool OnInputEnd_AsPlayer(OnInputEndData data)
	{
		if (data.LeftClick)
		{
			return ClickPart(data.position);
		}
		return false;
	}

	private bool ClickPart(TouchPosition position)
	{
		if (!RaycastPart(position, out var hit))
		{
			return false;
		}
		if (CanUsePart(hit.part))
		{
			_ = staging.editMode.Value;
			if (StagingDrawer.main.HasStageSelected())
			{
				StagingDrawer.main.TogglePartSelected(hit.part, playSound: true, createNewStep: false);
			}
			else if (!hit.part.HasModule<CannotClickModule>())
			{
				UseParts(false, (hit.part, hit.polygon));
			}
		}
		return true;
	}

	private bool RaycastPart(TouchPosition position, out PartHit hit)
	{
		return Part_Utility.RaycastParts(partHolder.GetArray(), position.World(0f), Mathf.Clamp((float)PlayerController.main.cameraDistance * 0.03f, 0f, 2f), out hit);
	}

	private bool CanUsePart(Part part)
	{
		if (!hasControl)
		{
			MsgDrawer.main.Log(Loc.main.No_Control_Msg);
			return false;
		}
		if (!physics.PhysicsMode)
		{
			MsgDrawer.main.Log(Loc.main.Cannot_Use_Part_While_Timewarping.InjectField(part.GetDisplayName(), "part"));
			return false;
		}
		return true;
	}

	public override float TryWorldSelect(TouchPosition data)
	{
		if (!partHolder.gameObject.activeSelf)
		{
			return float.PositiveInfinity;
		}
		Part_Utility.GetClosestPartToClick(data.World(0f), partHolder.parts.SelectMany((Part part) => from x in part.GetClickPolygons()
			select new PartHit(part, x)).ToArray(), out var bestDistance, out var _);
		return bestDistance;
	}

	public override bool CanTimewarp(I_MsgLogger logger, bool showSpeed)
	{
		bool flag = false;
		double timewarpRadius_AscendDescend = Planet.GetTimewarpRadius_AscendDescend(location.Value);
		if (location.Value.Radius < timewarpRadius_AscendDescend)
		{
			if (!IsOnSurface)
			{
				WorldTime.ShowCannotTimewarpBelowHeightMsg(timewarpRadius_AscendDescend - location.planet.Value.Radius, logger, showSpeed);
				return false;
			}
			if (location.velocity.Value.Mag_MoreThan(0.1))
			{
				WorldTime.ShowCannotTimewarpMsg(Loc.main.Cannot_Timewarp_While_Moving_On_Surface, logger);
				return false;
			}
		}
		if ((partHolder.GetModules<EngineModule>().Any((EngineModule a) => a.throttle_Out.Value > 0f) || partHolder.GetModules<BoosterModule>().Any((BoosterModule a) => a.throttle_Out.Value > 0f)) | flag)
		{
			WorldTime.ShowCannotTimewarpMsg(Loc.main.Cannot_Timewarp_While_Accelerating, logger);
			return false;
		}
		return true;
	}

	public static UsePartData[] UseParts(bool fromStaging, params (Part, PolygonData)[] regions)
	{
		UsePartData.SharedData sharedData = new UsePartData.SharedData(fromStaging);
		UsePartData[] array = new UsePartData[regions.Length];
		for (int i = 0; i < regions.Length; i++)
		{
			var (part, clickPolygon) = regions[i];
			array[i] = new UsePartData(sharedData, clickPolygon);
			if (part.onPartUsed != null && part.onPartUsed.GetPersistentEventCount() > 0)
			{
				part.onPartUsed.Invoke(array[i]);
			}
		}
		sharedData.onPostPartsActivation?.Invoke();
		return array;
	}

	public static void SetPlayerToBestControllable(params Rocket[] rockets)
	{
		List<Rocket> list = new List<Rocket>(rockets);
		list.Sort(delegate(Rocket a, Rocket b)
		{
			if ((bool)a.hasControl && (bool)b.hasControl)
			{
				double num = 0.0;
				double num2 = 0.0;
				ResourceModule[] globalGroups = a.resources.globalGroups;
				foreach (ResourceModule resourceModule in globalGroups)
				{
					num += resourceModule.resourcePercent.Value / (double)a.resources.globalGroups.Length;
				}
				globalGroups = b.resources.globalGroups;
				foreach (ResourceModule resourceModule2 in globalGroups)
				{
					num2 += resourceModule2.resourcePercent.Value / (double)b.resources.globalGroups.Length;
				}
				if (!(num2 > num))
				{
					return -1;
				}
				return 1;
			}
			return (b.hasControl ? 1 : 0) - (a.hasControl ? 1 : 0);
		});
		if (PlayerController.main.player.Value != list[0])
		{
			PlayerController.main.player.Value = list[0];
		}
	}

	private void Start_RocketInjector()
	{
		isPlayer.OnChange += new Action(Inject_IsPlayer);
		hasControl.OnChange += new Action(Inject_HasControl);
		throttle.throttleOn.OnChange += new Action(Inject_ThrottleOn);
		throttle.output_Throttle.OnChange += new Action(Inject_Throttle);
		output_TurnAxisTorque.OnChange += new Action(Inject_TurnAxisTorque);
		output_TurnAxisWheels.OnChange += new Action(Inject_TurnAxisWheels);
		output_DirectionalAxis.OnChange += new Action(Inject_DirectionalAxis);
		WorldTime.main.realtimePhysics.OnChange += new Action(OnRealtimePhysicsChange);
	}

	private void OnDestroy_RocketInjector()
	{
		WorldTime.main.realtimePhysics.OnChange -= new Action(OnRealtimePhysicsChange);
	}

	private void OnRealtimePhysicsChange()
	{
		if (!WorldTime.main.realtimePhysics && partHolder.GetModules<EngineModule>().Any((EngineModule engine) => engine.throttle_Out.Value > 0f))
		{
			throttle.throttleOn.Value = false;
		}
	}

	private void FixedUpdate()
	{
		Inject_Location();
		UpdateMass();
		UpdateMapIconRotation();
		ApplyTorque();
		output_DirectionalAxis.Value = (arrowkeys.rcs ? (arrowkeys.horizontalAxis.Value + arrowkeys.verticalAxis.Value) : Vector2.zero);
		FuelPipeModule.FixedUpdate_FuelPipeFlow(pipeFlows);
	}

	private void ApplyTorque()
	{
		float num = GetTorque();
		if (rb2d.mass > 200f)
		{
			num /= Mathf.Pow(rb2d.mass / 200f, 0.35f);
		}
		output_TurnAxisTorque.Value = GetTurnAxis(num);
		if (output_TurnAxisTorque.Value != 0f && rb2d.simulated)
		{
			rb2d.angularVelocity -= num * 57.29578f / rb2d.mass * output_TurnAxisTorque.Value * Time.fixedDeltaTime;
		}
		output_TurnAxisWheels.Value = arrowkeys.turnAxis;
	}

	private float GetTurnAxis(float torque)
	{
		if ((float)arrowkeys.turnAxis != 0f)
		{
			return arrowkeys.turnAxis;
		}
		if ((bool)hasControl)
		{
			return GetStopRotationTurnAxis(torque) * 0.9f;
		}
		return 0f;
	}

	private float GetStopRotationTurnAxis(float torque)
	{
		float angularVelocity = rb2d.angularVelocity;
		float num = torque * 57.29578f / rb2d.mass * Time.fixedDeltaTime;
		if (num == 0f)
		{
			return 0f;
		}
		return Mathf.Clamp(angularVelocity / num, -1f, 1f);
	}

	private void InjectPartDependencies()
	{
		Inject_Rocket();
		Inject_IsPlayer();
		Inject_HasControl();
		Inject_ThrottleOn();
		Inject_Throttle();
		Inject_TurnAxisTorque();
		Inject_TurnAxisWheels();
		Inject_DirectionalAxis();
		Inject_Location();
		Inject_Physics();
	}

	private void Inject_Rocket()
	{
		INJ_Rocket[] modules = partHolder.GetModules<INJ_Rocket>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].Rocket = this;
		}
	}

	private void Inject_IsPlayer()
	{
		INJ_IsPlayer[] modules = partHolder.GetModules<INJ_IsPlayer>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].IsPlayer = isPlayer;
		}
	}

	private void Inject_HasControl()
	{
		INJ_HasControl[] modules = partHolder.GetModules<INJ_HasControl>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].HasControl = hasControl;
		}
	}

	private void Inject_ThrottleOn()
	{
		INJ_ThrottleOn[] modules = partHolder.GetModules<INJ_ThrottleOn>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].ThrottleOn = throttle.throttleOn;
		}
	}

	private void Inject_Throttle()
	{
		INJ_Throttle[] modules = partHolder.GetModules<INJ_Throttle>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].Throttle = throttle.output_Throttle;
		}
	}

	private void Inject_TurnAxisTorque()
	{
		float turnAxis = output_TurnAxisTorque;
		INJ_TurnAxisTorque[] modules = partHolder.GetModules<INJ_TurnAxisTorque>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].TurnAxis = turnAxis;
		}
	}

	private void Inject_TurnAxisWheels()
	{
		float turnAxis = output_TurnAxisWheels;
		INJ_TurnAxisWheels[] modules = partHolder.GetModules<INJ_TurnAxisWheels>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].TurnAxis = turnAxis;
		}
	}

	private void Inject_DirectionalAxis()
	{
		Vector2 directionalAxis = output_DirectionalAxis;
		INJ_DirectionalAxis[] modules = partHolder.GetModules<INJ_DirectionalAxis>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].DirectionalAxis = directionalAxis;
		}
	}

	private void Inject_Location()
	{
		Location value = location.Value;
		INJ_Location[] modules = partHolder.GetModules<INJ_Location>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].Location = value;
		}
	}

	private void Inject_Physics()
	{
		INJ_Physics[] modules = partHolder.GetModules<INJ_Physics>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].Rb2d = rb2d;
		}
	}

	void I_Physics.OnCrashIntoPlanet()
	{
		RocketManager.DestroyRocket(this, DestructionReason.TerrainCollision);
	}

	void I_Physics.OnFixedUpdate(Vector2 gravity)
	{
		((I_Physics)this).LocalVelocity += gravity;
	}
}
