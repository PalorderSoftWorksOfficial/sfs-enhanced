using System;
using System.Collections;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Stats;
using SFS.Translations;
using SFS.World.Drag;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class Astronaut_EVA : Player, I_Physics
{
	public EVA_Resources resources;

	public Physics physics;

	public Arrowkeys arrowkeys;

	public AeroModule aero;

	public MapIcon mapIcon;

	public StatsRecorder stats;

	[Space]
	public AnimationCurve jumpVelocity;

	public AnimationCurve maxSpeed;

	public AnimationCurve runAcceleration;

	public AnimationCurve stopAcceleration;

	public MoveModule thruster_Left;

	public MoveModule thruster_Right;

	public MoveModule thruster_Up_Left;

	public MoveModule thruster_Up_Right;

	public MoveModule thruster_Down_Left;

	public MoveModule thruster_Down_Right;

	private MoveModule[] thrusters;

	[Space]
	public LayerMask collisions;

	public Transform sprite;

	public Rigidbody2D rb2d;

	[Space]
	public WorldSave.Astronauts.Data astronaut;

	public int facingDirection;

	public float airtime;

	public bool ragdoll;

	public float ragdollTime;

	private bool holdingUp;

	private float JumpVelocity => jumpVelocity.Evaluate(G);

	private float RunSpeed => maxSpeed.Evaluate(G);

	private float Acceleration => runAcceleration.Evaluate(G);

	private float Deceleration => stopAcceleration.Evaluate(G);

	private float G => (float)location.planet.Value.data.basics.gravity / 9.8f;

	public bool CanPickItselfUp
	{
		get
		{
			if (Mathf.Abs(rb2d.angularVelocity) < 3f)
			{
				return rb2d.velocity.magnitude < 0.2f;
			}
			return false;
		}
	}

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
			if (!rb2d.simulated)
			{
				rb2d.angularVelocity = 0f;
			}
		}
	}

	public Vector2 LocalPosition
	{
		get
		{
			return (Vector2)rb2d.transform.position + (Vector2)rb2d.transform.TransformVector(rb2d.centerOfMass);
		}
		set
		{
			Transform obj = rb2d.transform;
			Vector2 vector = (rb2d.position = value - (Vector2)rb2d.transform.TransformVector(rb2d.centerOfMass));
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

	private void Start()
	{
		thrusters = new MoveModule[6] { thruster_Left, thruster_Right, thruster_Up_Left, thruster_Up_Right, thruster_Down_Left, thruster_Down_Right };
		rb2d.centerOfMass = new Vector2(0f, 1.1f);
		rb2d.mass = 0.2f;
	}

	private void OnEnable()
	{
		AstronautManager.main.eva.Add(this);
	}

	private void OnDisable()
	{
		AstronautManager.main.eva.Remove(this);
	}

	private void OnFixedUpdate(Vector2 gravity)
	{
		mapIcon.SetRotation(rb2d.rotation - 90f);
		if (ragdoll)
		{
			rb2d.velocity += gravity;
			ragdollTime += Time.fixedDeltaTime;
			if (((!(ragdollTime > 1f) || !CanPickItselfUp) && !(ragdollTime > 5f)) || !astronaut.alive)
			{
				return;
			}
			SetRagdoll(enabled: false);
		}
		Vector2 value = arrowkeys.rawArrowkeysAxis.Value;
		bool flag = OnJumpKeyDown();
		float targetAngle = GetTargetAngle(location.Value);
		Double2 @double = WorldView.ToGlobalVelocity(rb2d.velocity);
		GetGroundRadius(out var groundAngleRadians, out var groundRadius);
		if (value.x != 0f)
		{
			facingDirection = (int)Mathf.Sign(value.x);
			sprite.localScale = new Vector3(Mathf.Abs(sprite.localScale.x) * Mathf.Sign(rb2d.transform.InverseTransformVector(@double).x), sprite.localScale.y);
		}
		bool flag2 = false;
		if (airtime == 0f)
		{
			Double2 double2 = @double.Rotate(0.0 - groundAngleRadians);
			float num = (float)double2.x;
			float num2 = value.x * RunSpeed;
			float num3 = ((Mathf.Abs(num) > Mathf.Abs(num2)) ? Deceleration : Acceleration) * Time.fixedDeltaTime;
			float num4 = Mathf.Clamp(num2 - num, 0f - num3, num3);
			@double += Double2.CosSin(groundAngleRadians, num4);
			Vector2 localPosition = LocalPosition;
			rb2d.rotation = Mathf.MoveTowardsAngle(rb2d.rotation, targetAngle, Time.fixedDeltaTime * 100f);
			rb2d.transform.rotation = Quaternion.Euler(0f, 0f, rb2d.rotation);
			rb2d.angularVelocity = 0f;
			LocalPosition = localPosition;
			if (flag && double2.y < 0.10000000149011612)
			{
				@double += arrowkeys.verticalAxis.Value * (JumpVelocity + 0.05f);
				flag2 = true;
			}
		}
		Double2 globalPosition = WorldView.ToGlobalPosition(LocalPosition);
		globalPosition -= (Vector3)globalPosition.normalized * ((float)location.planet.Value.data.basics.gravity * 0.02f * Time.fixedDeltaTime);
		groundRadius += (double)Mathf.Lerp(1.1f, 0.5f, Mathf.Sin((rb2d.rotation - targetAngle) * ((float)Math.PI / 180f)));
		bool flag3 = globalPosition.magnitude <= groundRadius;
		airtime = (flag3 ? 0f : (airtime + Time.fixedDeltaTime));
		if (flag3 && !flag2)
		{
			globalPosition = globalPosition.normalized * groundRadius;
			Double2 double3 = @double.Rotate(0.0 - groundAngleRadians);
			if (@double.magnitude > 3.5)
			{
				bool num5 = double3.x > 30.0 || 0.0 - double3.y > 15.0;
				double3 *= new Double2(0.3, -0.3);
				if (num5 && !SandboxSettings.main.settings.unbreakableParts)
				{
					StartDeathAnimation(0f);
				}
				else
				{
					SetRagdoll(enabled: true);
				}
			}
			else
			{
				double3.y = 0.0;
				@double = double3.Rotate(groundAngleRadians);
			}
		}
		else
		{
			@double += gravity;
		}
		LocalPosition = WorldView.ToLocalPosition(globalPosition);
		rb2d.velocity = WorldView.ToLocalVelocity(@double);
		MoveModule[] array = thrusters;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].targetTime.Value = 0f;
		}
		if (!ragdoll && airtime > 0.6f && (resources.fuelPercent.Value > 0.0 || SandboxSettings.main.settings.infiniteFuel))
		{
			RCS(value, targetAngle);
		}
	}

	private void RCS(Vector2 wasd, float targetAngle)
	{
		float num = ((resources.fuelPercent.Value > 0.0) ? 1 : 0);
		if (wasd.x < -0.1f)
		{
			thruster_Right.targetTime.Value += num;
		}
		else if (wasd.x > 0.1f)
		{
			thruster_Left.targetTime.Value += num;
		}
		if (wasd.y < -0.1f)
		{
			thruster_Up_Left.targetTime.Value += num;
			thruster_Up_Right.targetTime.Value += num;
		}
		else if (wasd.y > 0.1f)
		{
			thruster_Down_Left.targetTime.Value += num;
			thruster_Down_Right.targetTime.Value += num;
		}
		if (wasd.y > 0.1f && location.position.Value.Mag_LessThan(location.planet.Value.TimewarpRadius_Ascend))
		{
			float num2 = Mathf.Clamp((float)location.planet.Value.data.basics.gravity, 3f, 6f) * 0.5f * num;
			thruster_Down_Left.targetTime.Value += num2;
			thruster_Down_Right.targetTime.Value += num2;
		}
		float num3 = Mathf.DeltaAngle(rb2d.rotation, targetAngle);
		Debug.Log(num3);
		float num4 = Mathf.Sqrt(Mathf.Abs(num3)) * Mathf.Sign(num3) * 10f;
		float num5 = Mathf.Clamp((rb2d.angularVelocity - num4) * 0.01f, -0.3f, 0.3f);
		if (num5 < 0f)
		{
			thruster_Up_Left.targetTime.Value -= num5;
			thruster_Down_Right.targetTime.Value -= num5;
		}
		else if (num5 > 0f)
		{
			thruster_Down_Left.targetTime.Value += num5;
			thruster_Up_Right.targetTime.Value += num5;
		}
		float num6 = 0f;
		MoveModule[] array = thrusters;
		foreach (MoveModule moveModule in array)
		{
			if (moveModule.targetTime.Value > 0f)
			{
				float num7 = 0.2f * moveModule.targetTime.Value;
				rb2d.AddForceAtPosition(moveModule.transform.TransformVector(new Vector3(0f - num7, 0f, 0f)), moveModule.transform.position, ForceMode2D.Force);
				num6 += num7;
			}
		}
		if (num6 > 0f)
		{
			float num8 = num6 * Time.fixedDeltaTime;
			resources.ConsumeFuel((double)num8 / 200.0);
		}
	}

	private bool OnJumpKeyDown()
	{
		bool flag = arrowkeys.rawArrowkeysAxis.Value.y > 0.5f;
		bool result = !holdingUp & flag;
		holdingUp = flag;
		return result;
	}

	public void SetRagdoll(bool enabled)
	{
		ragdoll = enabled;
		int layer = LayerMask.NameToLayer(enabled ? "Non Colliding Parts" : "Astronaut");
		Collider2D[] componentsInChildren = rb2d.GetComponentsInChildren<Collider2D>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = layer;
		}
		ragdollTime = 0f;
	}

	private void StartDeathAnimation(float startTime)
	{
		hasControl.Value = false;
		astronaut.alive = false;
		SetRagdoll(enabled: true);
		StartCoroutine(DeathAnimation(startTime, delegate
		{
			AstronautManager.DestroyEVA(this, death: true);
		}));
	}

	private static IEnumerator DeathAnimation(float startTime, Action delete)
	{
		AstronautManager.main.fadeToBlack.time.Value = startTime;
		AstronautManager.main.fadeToBlack.targetTime.Value = 1f;
		AstronautManager.main.fadeToBlack.animationTime = 8f;
		yield return new WaitForSeconds((0.7f - startTime) * 8f);
		delete();
	}

	void I_Physics.OnFixedUpdate(Vector2 gravity)
	{
		OnFixedUpdate(gravity);
	}

	void I_Physics.OnCrashIntoPlanet()
	{
		AstronautManager.DestroyEVA(this, death: true);
	}

	public override float TryWorldSelect(TouchPosition data)
	{
		if (!rb2d.gameObject.activeSelf)
		{
			return float.PositiveInfinity;
		}
		return (rb2d.worldCenterOfMass - data.World(0f)).magnitude - 0.5f;
	}

	public override bool CanTimewarp(I_MsgLogger logger, bool showSpeed)
	{
		double timewarpRadius_AscendDescend = Planet.GetTimewarpRadius_AscendDescend(location.Value);
		if (location.Value.Radius < timewarpRadius_AscendDescend)
		{
			if (airtime > 0f)
			{
				WorldTime.ShowCannotTimewarpBelowHeightMsg(timewarpRadius_AscendDescend - location.planet.Value.Radius, logger, showSpeed);
				return false;
			}
			if (rb2d.velocity.magnitude > 1E-06f)
			{
				WorldTime.ShowCannotTimewarpMsg(Loc.main.Cannot_Timewarp_While_Moving_On_Surface, logger);
				return false;
			}
		}
		return true;
	}

	public override void ClampTrackingOffset(ref Vector2 trackingOffset, float cameraDistance)
	{
		trackingOffset *= 0f;
	}

	public override float GetSizeRadius()
	{
		return 3f;
	}

	public override bool OnInputEnd_AsPlayer(OnInputEndData data)
	{
		if (!hasControl.Value)
		{
			return false;
		}
		if (data.LeftClick)
		{
			Vector2 vector = data.position.World(0f);
			foreach (Rocket rocket in GameManager.main.rockets)
			{
				if (rocket.physics.loader.Loaded && Part_Utility.RaycastParts(rocket.partHolder.GetArray(), vector, (float)PlayerController.main.cameraDistance * 0.03f, out var hit) && hit.part.HasModule<CrewModule>())
				{
					hit.part.GetModules<CrewModule>()[0].OpenPartMenu(canBoardWorld: true);
					return true;
				}
			}
			if (RockSelector.main.TrySelect(vector))
			{
				return true;
			}
		}
		return false;
	}

	private void GetGroundRadius(out double groundAngleRadians, out double groundRadius)
	{
		GetGroundRadius(new Location(location.planet.Value, WorldView.ToGlobalPosition(LocalPosition)), out groundAngleRadians, out groundRadius);
	}

	public static void GetGroundRadius(Location location, out double groundAngleRadians, out double groundRadius)
	{
		Double2 position = location.position;
		groundRadius = location.planet.Radius + location.planet.GetTerrainHeightAtAngle(position.AngleRadians);
		groundAngleRadians = location.planet.GetTerrainNormal(position).AngleRadians;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(WorldView.ToLocalPosition(position) + position.normalized * 10.0, -position.normalized, 20f, AstronautManager.main.astronautPrefab.collisions);
		if ((bool)raycastHit2D)
		{
			double num = position.magnitude - (double)(raycastHit2D.distance - 10f);
			if (num > groundRadius)
			{
				groundRadius = num;
				groundAngleRadians = (double)Mathf.Atan2(raycastHit2D.normal.y, raycastHit2D.normal.x) - Math.PI / 2.0;
			}
		}
	}

	public static float GetTargetAngle(Location loc)
	{
		if (!loc.position.Mag_LessThan(Planet.GetTimewarpRadius_AscendDescend(loc)))
		{
			return 0f;
		}
		return (float)loc.position.AngleDegrees - 90f;
	}
}
