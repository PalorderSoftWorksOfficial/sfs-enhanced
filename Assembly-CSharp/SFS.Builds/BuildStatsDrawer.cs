using System;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Builds;

public class BuildStatsDrawer : MonoBehaviour
{
	public PartHolder partHolder;

	public Text stats;

	public TextAdapter massText;

	public TextAdapter thrustText;

	public TextAdapter thrustToWeightText;

	public TextAdapter heightText;

	public TextAdapter partCountText;

	public float mass;

	public float thrust;

	public float TWR;

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
	}

	private void MarkDirty()
	{
		base.enabled = true;
	}

	private void LateUpdate()
	{
		base.enabled = false;
		Draw();
	}

	private void Draw()
	{
		mass = GetMass();
		thrust = GetThrust();
		TWR = ((mass > 0f) ? (thrust / mass) : 0f);
		massText.Text = mass.ToString() + Loc.main.Mass_Unit;
		thrustText.Text = thrust.ToString() + Loc.main.Mass_Unit;
		thrustToWeightText.Text = TWR.ToString(2, forceDecimals: true);
		heightText.Text = GetHeight().ToDistanceString();
		partCountText.Text = partHolder.parts.Count.ToString();
	}

	private float GetMass()
	{
		return partHolder.parts.Sum((Part a) => a.mass.Value);
	}

	private float GetThrust()
	{
		float num = 0f;
		Part[] array = partHolder.GetArray();
		(Part part, (ConvexPolygon[] polygons, bool isFront))[] colliders = Part_Utility.GetBuildColliderPolygons_WithPart(array);
		(bool, Rect)[] bounds = new(bool, Rect)[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 min = Vector2.positiveInfinity;
			Vector2 max = Vector2.negativeInfinity;
			colliders[i].Item2.polygons.ForEach(delegate(ConvexPolygon poly)
			{
				Part_Utility.ExpandToFitPoint(ref min, ref max, poly.points);
			});
			min -= Vector2.one;
			max += Vector2.one;
			bounds[i] = (!double.IsPositiveInfinity(min.x), new Rect(min, max - min));
		}
		EngineModule[] modules = partHolder.GetModules<EngineModule>();
		foreach (EngineModule engineModule in modules)
		{
			if (IsThrustNotBlocked(engineModule.GetComponentInParent<Part>(), engineModule.transform, engineModule.thrustNormal.Value, engineModule.thrustPosition.Value))
			{
				num += engineModule.thrust.Value;
			}
		}
		BoosterModule[] modules2 = partHolder.GetModules<BoosterModule>();
		foreach (BoosterModule boosterModule in modules2)
		{
			if (IsThrustNotBlocked(boosterModule.GetComponentInParent<Part>(), boosterModule.transform, boosterModule.thrustVector.Value, boosterModule.thrustPosition.Value))
			{
				num += boosterModule.thrustVector.Value.magnitude;
			}
		}
		return num;
		bool IsThrustNotBlocked(Part thrusterPart, Transform a, Vector2 thrustVector, Vector2 position)
		{
			Vector2 vector = a.TransformVector(thrustVector).normalized * -10f;
			position = a.TransformPoint(position);
			Vector2 min2 = Vector2.positiveInfinity;
			Vector2 max2 = Vector2.negativeInfinity;
			Part_Utility.ExpandToFitPoint(ref min2, ref max2, position, position + vector);
			min2 -= Vector2.one;
			max2 += Vector2.one;
			(bool, Rect) tuple = (!double.IsPositiveInfinity(min2.x), new Rect(min2, max2 - min2));
			for (int j = 0; j < colliders.Length; j++)
			{
				var (part, (b, flag)) = colliders[j];
				if (tuple.Item1 && bounds[j].Item1 && tuple.Item2.Overlaps(bounds[j].Item2) && part != thrusterPart && flag == thrusterPart.IsFront() && Polygon.Intersect(position, vector, b, -0.1f))
				{
					return false;
				}
			}
			return true;
		}
	}

	private double GetHeight()
	{
		Rect bounds;
		return Part_Utility.GetBuildColliderBounds_WorldSpace(out bounds, useLaunchBounds: true, partHolder.GetArray()) ? bounds.height : 0f;
	}
}
