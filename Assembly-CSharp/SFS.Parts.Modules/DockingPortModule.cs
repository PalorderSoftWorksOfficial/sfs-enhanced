using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class DockingPortModule : MonoBehaviour, Rocket.INJ_Rocket
{
	public DockingPortTrigger trigger;

	public SurfaceData occupationSurface;

	public float dockDistance;

	public float pullDistance;

	public float pullForce;

	public Float_Reference forceMultiplier;

	public Bool_Local isOccupied;

	public Bool_Local isOnCooldown;

	public Bool_Local isDockable;

	private Part part;

	private List<DockingPortModule> portsInRange = new List<DockingPortModule>();

	public Rocket Rocket { get; set; }

	private void Awake()
	{
		part = base.transform.GetComponentInParentTree<Part>();
	}

	private void Start()
	{
		isOccupied.OnChange += new Action(OnIsOccupiedChange);
		isOccupied.OnChange += new Action(UpdateIsDockable);
		isOnCooldown.OnChange += new Action(UpdateIsDockable);
		isDockable.OnChange += new Action<bool>(OnIsDockableChange);
	}

	public void UpdateOccupied()
	{
		Line2[] mySurfaces = occupationSurface.surfaces[0].GetSurfacesWorld();
		isOccupied.Value = Rocket.jointsGroup.GetConnectedJoints(part).Any((PartJoint joint) => SurfaceUtility.SurfacesConnect(joint.GetOtherPart(part), mySurfaces, out var _, out var _));
	}

	private void OnIsOccupiedChange()
	{
		if (!isOccupied)
		{
			if (IsInvoking("EndCooldown"))
			{
				CancelInvoke("EndCooldown");
			}
			isOnCooldown.Value = true;
			Invoke("EndCooldown", 2f);
		}
	}

	private void EndCooldown()
	{
		isOnCooldown.Value = false;
	}

	private void UpdateIsDockable()
	{
		isDockable.Value = !isOccupied && !isOnCooldown;
	}

	private void OnIsDockableChange(bool newValue)
	{
		if (!newValue)
		{
			portsInRange.Clear();
		}
		trigger.gameObject.SetActive(newValue);
	}

	private void FixedUpdate()
	{
		if (!isDockable)
		{
			return;
		}
		foreach (DockingPortModule item in portsInRange)
		{
			if ((bool)item.isDockable)
			{
				if (Vector2.Distance(base.transform.position, item.transform.position) <= dockDistance)
				{
					Dock(item);
					break;
				}
				Vector3 normalized = (item.transform.position - base.transform.position).normalized;
				Rocket.rb2d.AddForceAtPosition(pullForce * forceMultiplier.Value * 2f * normalized, base.transform.position);
			}
		}
	}

	private void Dock(DockingPortModule otherPort)
	{
		if ((bool)otherPort.Rocket.isPlayer)
		{
			return;
		}
		Vector2 vector = Vector2.up * part.orientation;
		Vector2 vector2 = Vector2.up * otherPort.part.orientation;
		Orientation orientation = new Orientation(1f, 1f, (int)(Mathf.Atan2(vector.y, vector.x) * 57.29578f - Mathf.Atan2(vector2.y, vector2.x) * 57.29578f) + 180);
		orientation.z = Mathf.RoundToInt(orientation.z / 90f) * 90;
		foreach (Part part in otherPort.Rocket.partHolder.parts)
		{
			part.orientation.orientation.Value += orientation;
		}
		foreach (PartJoint joint in otherPort.Rocket.jointsGroup.joints)
		{
			joint.anchor *= orientation;
		}
		Vector2 worldCenterOfMass = Rocket.rb2d.worldCenterOfMass;
		RocketManager.MergeRockets(Rocket, this.part, otherPort.Rocket, otherPort.part, Vector2.zero);
		if ((bool)Rocket.isPlayer)
		{
			PlayerController.main.SetOffset(worldCenterOfMass - Rocket.rb2d.worldCenterOfMass, 1f);
		}
	}

	public void AddPort(DockingPortModule port)
	{
		if (IsValidPort(port) && !portsInRange.Contains(port))
		{
			portsInRange.Add(port);
		}
	}

	public void RemovePort(DockingPortModule port)
	{
		if (IsValidPort(port) && portsInRange.Contains(port))
		{
			portsInRange.Remove(port);
		}
	}

	private bool IsValidPort(DockingPortModule port)
	{
		if (port != this && port.Rocket != null)
		{
			return port.Rocket != Rocket;
		}
		return false;
	}

	private void OnDestroy()
	{
		foreach (DockingPortModule item in portsInRange)
		{
			item.RemovePort(this);
		}
	}

	public void Draw(List<DockingPortModule> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.build)
		{
			drawer.DrawSlider(71, GetForce, GetMaxForce, () => forceMultiplier.Value, SetForce, delegate(Action x)
			{
				forceMultiplier.OnChange += x;
			}, delegate(Action x)
			{
				forceMultiplier.OnChange -= x;
			});
		}
		string GetForce()
		{
			return (pullForce * forceMultiplier.Value * 2f).ToMagnetForceString(forceDecimals: true);
		}
		string GetMaxForce()
		{
			return pullForce.ToMagnetForceString(forceDecimals: true);
		}
		void SetForce(float value, bool touchStart)
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				foreach (DockingPortModule module in modules)
				{
					module.forceMultiplier.Value = value;
				}
			}, touchStart);
		}
	}
}
