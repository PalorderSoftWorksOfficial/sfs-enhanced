using UnityEngine;

public class ConvexPolygon
{
	public readonly Vector2[] points;

	public readonly Vector2[] surfaces;

	public ConvexPolygon(Vector2[] points)
	{
		this.points = points;
		surfaces = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++)
		{
			surfaces[i] = points[i] - points[(i + 1) % points.Length];
		}
	}

	public static bool Intersect(ConvexPolygon A, ConvexPolygon B, float overlapThreshold)
	{
		Vector2[] array = A.surfaces;
		for (int i = 0; i < array.Length; i++)
		{
			if (Intersect(array[i], A, B, overlapThreshold))
			{
				return false;
			}
		}
		array = B.surfaces;
		for (int i = 0; i < array.Length; i++)
		{
			if (Intersect(array[i], A, B, overlapThreshold))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Intersect(Vector2 edge, ConvexPolygon A, ConvexPolygon B, float overlapThreshold)
	{
		Vector2 normalized = new Vector2(0f - edge.y, edge.x).normalized;
		A.ProjectPolygon(normalized, out var min, out var max);
		B.ProjectPolygon(normalized, out var min2, out var max2);
		return IntervalDistance(min, max, min2, max2) > overlapThreshold;
	}

	private void ProjectPolygon(Vector2 axis, out float min, out float max)
	{
		min = (max = Vector2.Dot(points[0], axis));
		Vector2[] array = points;
		for (int i = 0; i < array.Length; i++)
		{
			float num = Vector2.Dot(array[i], axis);
			if (num < min)
			{
				min = num;
			}
			else if (num > max)
			{
				max = num;
			}
		}
	}

	public bool Intersects(Vector2 point_A, Vector2 point_B, float overlapThreshold)
	{
		for (int i = 0; i < surfaces.Length; i++)
		{
			if (Intersects(surfaces[i], point_A, point_B, overlapThreshold))
			{
				return false;
			}
		}
		if (Intersects(point_A - point_B, point_A, point_B, overlapThreshold))
		{
			return false;
		}
		return true;
	}

	private bool Intersects(Vector2 edge, Vector2 point_A, Vector2 point_B, float overlapThreshold)
	{
		Vector2 normalized = new Vector2(0f - edge.y, edge.x).normalized;
		ProjectPolygon(normalized, out var min, out var max);
		ProjectSurface(point_A, point_B, normalized, out var min2, out var max2);
		return IntervalDistance(min, max, min2, max2) > overlapThreshold;
	}

	private static void ProjectSurface(Vector2 point_A, Vector2 point_B, Vector2 axis, out float min, out float max)
	{
		float a = Vector2.Dot(point_A, axis);
		float b = Vector2.Dot(point_B, axis);
		min = Mathf.Min(a, b);
		max = Mathf.Max(a, b);
	}

	public float GetDistanceToPolygon(Vector2 point)
	{
		if (surfaces.Length == 0)
		{
			return float.PositiveInfinity;
		}
		float num = float.NegativeInfinity;
		Vector2[] array = surfaces;
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 vector = array[i];
			Vector2 normalized = new Vector2(0f - vector.y, vector.x).normalized;
			ProjectPolygon(normalized, out var min, out var max);
			float num2 = Vector2.Dot(point, normalized);
			float num3 = Mathf.Max(min - num2, num2 - max);
			if (num3 > num)
			{
				num = num3;
			}
		}
		return num;
	}

	private static float IntervalDistance(float minA, float maxA, float minB, float maxB)
	{
		if (minA < minB)
		{
			return minB - maxA;
		}
		return minA - maxB;
	}
}
