using System;
using System.Collections.Generic;
using System.Linq;
using SFS.World;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ColliderModule : MonoBehaviour, Rocket.INJ_Rocket
{
	public enum ImpactTolerance
	{
		Low,
		Medium,
		High,
		Wheel
	}

	public ImpactTolerance impactTolerance = ImpactTolerance.Medium;

	public Collider2D ownEngineNozzle;

	private int planetLayer;

	private int flameHeatLayer;

	private HeatModuleBase heatModule;

	private Rocket rocket;

	Rocket Rocket.INJ_Rocket.Rocket
	{
		set
		{
			rocket = value;
		}
	}

	private float MaxImpactTolerance => impactTolerance switch
	{
		ImpactTolerance.Low => 2.5f, 
		ImpactTolerance.Medium => 5.5f, 
		ImpactTolerance.High => 12.5f, 
		ImpactTolerance.Wheel => 50.5f, 
		_ => throw new Exception(), 
	};

	private void Start()
	{
		planetLayer = LayerMask.NameToLayer("Celestial Body");
		flameHeatLayer = LayerMask.NameToLayer("Flame Trigger");
		heatModule = base.transform.GetComponentInParentTree<HeatModuleBase>();
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (SandboxSettings.main.settings.unbreakableParts)
		{
			return;
		}
		List<ContactPoint2D> list = new List<ContactPoint2D>();
		collision.GetContacts(list);
		if (list.Max((ContactPoint2D x) => (Vector2.Dot(x.normal, collision.relativeVelocity) + collision.relativeVelocity.magnitude * 0.5f) / 1.5f) < MaxImpactTolerance)
		{
			return;
		}
		GetPart(out var part);
		ColliderModule component = collision.otherCollider.GetComponent<ColliderModule>();
		if (component != null && (rocket.collisionImmunity > Time.time || part.mass.Value > component.GetComponentInParent<Part>().mass.Value * 5f))
		{
			return;
		}
		DestructionReason reason = DestructionReason.RocketCollision;
		if (collision.gameObject.layer == planetLayer)
		{
			float impactVelocity = list.Max((ContactPoint2D x) => collision.relativeVelocity.magnitude);
			rocket.stats.OnCrash(impactVelocity);
			reason = DestructionReason.TerrainCollision;
		}
		part.DestroyPart(rocket, updateJoints: true, reason);
	}

	private void GetPart(out Part part)
	{
		Transform parent = base.transform;
		do
		{
			part = parent.GetComponent<Part>();
			if (part != null)
			{
				return;
			}
			parent = parent.parent;
		}
		while (!(parent == null));
		throw new Exception("Could not find part in parent tree");
	}

	private void OnTriggerStay2D(Collider2D other)
	{
		if (other.gameObject.layer == flameHeatLayer && other != ownEngineNozzle)
		{
			rocket.aero.heatManager.HeatPart(heatModule);
		}
	}
}
