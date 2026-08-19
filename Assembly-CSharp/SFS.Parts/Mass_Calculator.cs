using System;
using SFS.World;
using UnityEngine;

namespace SFS.Parts;

public class Mass_Calculator : MonoBehaviour
{
	public PartHolder partHolder;

	private bool dirty = true;

	private float mass;

	private Vector2 centerOfMass;

	private void Start()
	{
		partHolder.TrackParts(delegate(Part addedPart)
		{
			addedPart.mass.OnChange += new Action(MarkDirty);
			addedPart.centerOfMass.OnChange += new Action(MarkDirty);
		}, delegate(Part removedPart)
		{
			removedPart.mass.OnChange -= new Action(MarkDirty);
			removedPart.centerOfMass.OnChange -= new Action(MarkDirty);
		}, MarkDirty);
		if ((object)WorldTime.main != null)
		{
			WorldTime.main.realtimePhysics.OnChange += new Action(MarkDirty);
		}
	}

	private void MarkDirty()
	{
		if ((object)WorldTime.main == null || (bool)WorldTime.main.realtimePhysics)
		{
			dirty = true;
		}
	}

	public float GetMass()
	{
		Calculate();
		return mass;
	}

	public Vector2 GetCenterOfMass()
	{
		Calculate();
		return centerOfMass;
	}

	private void Calculate()
	{
		if (!dirty)
		{
			return;
		}
		mass = 0f;
		centerOfMass = Vector2.zero;
		foreach (Part part in partHolder.parts)
		{
			mass += part.mass.Value;
			centerOfMass += (part.Position + part.centerOfMass.Value * part.orientation) * part.mass.Value;
		}
		centerOfMass /= mass;
		dirty = false;
	}
}
