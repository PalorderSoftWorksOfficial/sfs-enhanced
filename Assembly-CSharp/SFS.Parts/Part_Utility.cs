using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Parts;

public static class Part_Utility
{
	public static bool RaycastParts(Part[] parts, Vector2 worldPoint, float threshold, out PartHit hit)
	{
		threshold *= 0.1f;
		PartHit[] array = parts.SelectMany((Part part) => from x in part.GetClickPolygons()
			where Polygon.GetDistanceToPolygons(worldPoint, x.polygon.GetConvexPolygonsWorld(x.transform)) <= threshold
			select new PartHit(part, x)).ToArray();
		if (array.Length != 0)
		{
			if (array.Length == 1)
			{
				hit = array[0];
				return true;
			}
			array = GetHighestDepthAtPoint(worldPoint, array, threshold);
			if (array.Length == 1)
			{
				hit = array[0];
				return true;
			}
			GetClosestPartToClick(worldPoint, array, out var bestDistance, out var bestHit);
			if (bestDistance < threshold)
			{
				hit = bestHit;
				return true;
			}
		}
		hit = null;
		return false;
	}

	private static PartHit[] GetHighestDepthAtPoint(Vector2 worldPoint, PartHit[] hits, float threshold)
	{
		List<PartHit> list = new List<PartHit>();
		float num = float.NegativeInfinity;
		foreach (PartHit partHit in hits)
		{
			PolygonData[] modules = partHit.part.GetModules<PolygonData>();
			foreach (PolygonData polygonData in modules)
			{
				if (!partHit.polygon.Click || Polygon.GetDistanceToPolygons(worldPoint, polygonData.polygon.GetConvexPolygonsWorld(polygonData.transform)) > threshold)
				{
					continue;
				}
				if (!partHit.polygon.Raycast(polygonData.transform.InverseTransformPoint(worldPoint), out var depth))
				{
					depth = float.NegativeInfinity;
				}
				if (!(depth < num))
				{
					if (depth > num)
					{
						list.Clear();
					}
					list.Add(partHit);
					num = depth;
				}
			}
		}
		return list.ToArray();
	}

	public static void GetClosestPartToClick(Vector2 point, PartHit[] hits, out float bestDistance, out PartHit bestHit)
	{
		bestDistance = float.PositiveInfinity;
		bestHit = null;
		foreach (PartHit partHit in hits)
		{
			float distanceToPolygons = Polygon.GetDistanceToPolygons(point, partHit.polygon.polygon.GetConvexPolygonsWorld(partHit.polygon.transform));
			if (distanceToPolygons < bestDistance)
			{
				bestDistance = distanceToPolygons;
				bestHit = partHit;
			}
		}
	}

	public static bool CollidersIntersect(Part part_A, Part part_B)
	{
		var (a, flag) = part_A.GetBuildColliderPolygons();
		var (b, flag2) = part_B.GetBuildColliderPolygons();
		if (flag == flag2)
		{
			return Polygon.Intersect(a, b, -0.08f);
		}
		return false;
	}

	public static (ConvexPolygon[] normal, ConvexPolygon[] front) GetBuildColliderPolygons(Part[] parts)
	{
		List<ConvexPolygon> list = new List<ConvexPolygon>();
		List<ConvexPolygon> list2 = new List<ConvexPolygon>();
		for (int i = 0; i < parts.Length; i++)
		{
			(ConvexPolygon[], bool isFront) buildColliderPolygons = parts[i].GetBuildColliderPolygons();
			var (collection, _) = buildColliderPolygons;
			(buildColliderPolygons.isFront ? list2 : list).AddRange(collection);
		}
		return (normal: list.ToArray(), front: list2.ToArray());
	}

	public static (Part part, (ConvexPolygon[], bool isFront))[] GetBuildColliderPolygons_WithPart(Part[] parts)
	{
		return parts.Select((Part part) => (part: part, part.GetBuildColliderPolygons())).ToArray();
	}

	public static void PositionParts(Vector2 position, Vector2 pivot, bool round, bool useLaunchBounds, params Part[] parts)
	{
		if (GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds, parts))
		{
			Vector2 vector = bounds.position + bounds.size * pivot;
			OffsetPartPosition(position - vector, round, parts);
		}
	}

	public static void OffsetPartPosition(Vector2 offset, bool round, params Part[] parts)
	{
		if (!(offset.magnitude < 0.001f))
		{
			if (round)
			{
				offset = offset.Round(0.5f);
			}
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i].transform.localPosition += (Vector3)offset;
			}
		}
	}

	public static void CenterParts(Part[] parts, Vector2 boxSize)
	{
		if (GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, parts))
		{
			OffsetPartPosition(boxSize / 2f - bounds.center, round: true, parts);
		}
	}

	public static void ApplyOrientationChange(Orientation change, Vector2 pivot, IEnumerable<Part> parts)
	{
		foreach (Part part in parts)
		{
			part.orientation.orientation.Value += (part.orientation.orientation.Value.InversedAxis() ? new Orientation(change.y, change.x, change.z) : change);
			part.transform.localPosition = ((Vector2)part.transform.localPosition - pivot) * change + pivot;
			part.RegenerateMesh();
		}
	}

	public static bool GetFramingBounds_WorldSpace(out Rect bounds, Part[] parts)
	{
		Vector2 min = Vector2.positiveInfinity;
		Vector2 max = Vector2.negativeInfinity;
		foreach (Part part in parts)
		{
			if (GetFramingBounds_WorldSpace(out var bounds2, part))
			{
				ExpandToFitPoint(ref min, ref max, bounds2.min, bounds2.max);
			}
		}
		bounds = new Rect(min, max - min);
		return !double.IsPositiveInfinity(min.x);
	}

	public static bool GetFramingBounds_WorldSpace(out Rect bounds, Part part)
	{
		if (part.HasModule<FramingOverwrite>())
		{
			bounds = part.GetModules<FramingOverwrite>()[0].GetBounds_WorldSpace();
			return true;
		}
		return GetBounds_WorldSpace(out bounds, (from x in part.GetModules<PolygonData>()
			where x.BuildCollider || x.Click
			select x).Concat(from framingColliderBounds in part.GetModules<FramingColliderBounds>()
			select framingColliderBounds.shape).ToArray());
	}

	public static bool GetBuildColliderBounds_WorldSpace(out Rect bounds, bool useLaunchBounds, params Part[] parts)
	{
		return GetBounds_WorldSpace(out bounds, (PolygonData x) => x.BuildCollider || x.Click, useLaunchBounds, useFramingBounds: false, parts);
	}

	public static bool GetBounds_WorldSpace(out Rect bounds, IEnumerable<PolygonData> polygons)
	{
		Vector2 min = Vector2.positiveInfinity;
		Vector2 max = Vector2.negativeInfinity;
		polygons.ForEach(delegate(PolygonData poly)
		{
			ExpandToFitPoint(ref min, ref max, poly.polygon.GetVerticesWorld(poly.transform));
		});
		bounds = new Rect(min, max - min);
		return !double.IsPositiveInfinity(min.x);
	}

	public static bool GetBounds_WorldSpace(out Rect bounds, Func<PolygonData, bool> usePolygon, bool useLaunchBounds, bool useFramingBounds, params Part[] parts)
	{
		List<PolygonData> list = GetModules<PolygonData>(parts).Where(usePolygon).ToList();
		if (useLaunchBounds)
		{
			list.AddRange(from x in GetModules<LaunchColliderBounds>(parts)
				select x.shape);
		}
		if (useFramingBounds)
		{
			list.AddRange(from x in GetModules<FramingColliderBounds>(parts)
				select x.shape);
		}
		return GetBounds_WorldSpace(out bounds, list);
	}

	public static void ExpandToFitPoint(ref Vector2 min, ref Vector2 max, params Vector2[] points)
	{
		for (int i = 0; i < points.Length; i++)
		{
			Vector2 vector = points[i];
			if (vector.x < min.x)
			{
				min.x = vector.x;
			}
			if (vector.y < min.y)
			{
				min.y = vector.y;
			}
			if (vector.x > max.x)
			{
				max.x = vector.x;
			}
			if (vector.y > max.y)
			{
				max.y = vector.y;
			}
		}
	}

	public static IEnumerable<T> GetModules<T>(IEnumerable<Part> parts)
	{
		return parts.SelectMany((Part x) => x.GetModules<T>());
	}
}
