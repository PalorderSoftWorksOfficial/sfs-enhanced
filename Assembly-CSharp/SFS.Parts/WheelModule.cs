using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts;

public class WheelModule : MonoBehaviour, Rocket.INJ_TurnAxisWheels, Rocket.INJ_Physics
{
	public float power;

	public float traction;

	public float maxAngularVelocity;

	public float wheelSize;

	public float angularVelocity;

	public Bool_Reference on;

	public float TurnAxis { get; set; }

	public Rigidbody2D Rb2d { get; set; }

	public void Draw(List<WheelModule> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.build || settings.game)
		{
			drawer.DrawToggle(-1, () => Loc.main.Wheel_On_Label, Toggle, () => on.Value, delegate(Action update)
			{
				on.OnChange += update;
			}, delegate(Action update)
			{
				on.OnChange -= update;
			});
		}
		void Toggle()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				bool value = !on.Value;
				foreach (WheelModule module in modules)
				{
					module.on.Value = value;
				}
			});
		}
	}

	public void ToggleEnabled()
	{
		on.Value = !on.Value;
		MsgDrawer.main.Log(Loc.main.Wheel_Module_State.InjectField(on.Value.State_ToOnOff(), "state"));
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		float num = angularVelocity * ((float)Math.PI / 180f) * wheelSize;
		Vector2 vector = Quaternion.Euler(0f, 0f, -90f) * collision.contacts[0].normal;
		Vector2 vector2 = collision.contacts[0].relativeVelocity - vector * num;
		float magnitude = vector2.magnitude;
		float num2 = 1f;
		num2 = num2 * 0.1f * (float)WorldView.main.ViewLocation.planet.data.basics.gravity;
		float num3 = traction / Rb2d.mass * Time.fixedDeltaTime * 10f;
		if (num3 > 1f)
		{
			num2 /= num3;
		}
		Rb2d.AddForceAtPosition(vector2 * traction * num2, base.transform.position);
		if (collision.rigidbody != null)
		{
			collision.rigidbody.AddForceAtPosition(-vector2 * traction * num2, collision.contacts[0].point);
		}
		float num4 = magnitude * traction * num2;
		Vector2 vector3 = collision.contacts[0].relativeVelocity - vector * ((angularVelocity + num4) * ((float)Math.PI / 180f) * wheelSize);
		Vector2 vector4 = collision.contacts[0].relativeVelocity - vector * ((angularVelocity - num4) * ((float)Math.PI / 180f) * wheelSize);
		float sqrMagnitude = vector3.sqrMagnitude;
		float sqrMagnitude2 = vector4.sqrMagnitude;
		if (sqrMagnitude > sqrMagnitude2)
		{
			angularVelocity -= magnitude * traction * num2;
		}
		else
		{
			angularVelocity += magnitude * traction * num2;
		}
	}

	private void Update()
	{
		float num = (on.Value ? (0f - TurnAxis) : 0f);
		if (num == 0f)
		{
			num = Mathf.Clamp((0f - angularVelocity) / Time.deltaTime / power, on.Value ? (-0.25f) : (-0.05f), on.Value ? 0.25f : 0.05f);
		}
		if (num != 0f)
		{
			angularVelocity = Mathf.Clamp(angularVelocity + num * power * Time.deltaTime, 0f - maxAngularVelocity, maxAngularVelocity);
		}
		if (float.IsNaN(angularVelocity))
		{
			angularVelocity = 0f;
		}
		if (angularVelocity != 0f)
		{
			base.transform.eulerAngles = new Vector3(0f, 0f, base.transform.eulerAngles.z + angularVelocity * Time.deltaTime);
		}
	}
}
