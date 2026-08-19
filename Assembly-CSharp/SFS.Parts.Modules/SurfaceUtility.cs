using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

public static class SurfaceUtility
{
	private const float MaxDistance = 0.05f;

	private const float MinOverlap = 0.05f;

	private const int MaxAngleDiff = 5;

	public static bool SurfacesConnect(Part A, SurfaceData B, out float overlap, out Vector2 center)
	{
		List<Line2> list = new List<Line2>();
		foreach (Surfaces surface in B.surfaces)
		{
			list.AddRange(surface.GetSurfacesWorld());
		}
		return SurfacesConnect(list.ToArray(), A.GetAttachmentSurfacesWorld(), out overlap, out center);
	}

	public static bool SurfacesConnect(Part A, Line2[] B, out float overlap, out Vector2 center)
	{
		return SurfacesConnect(A.GetAttachmentSurfacesWorld(), B, out overlap, out center);
	}

	public static bool SurfacesConnect(SurfaceData A, SurfaceData B, out float overlap, out Vector2 center)
	{
		return SurfacesConnect(A.surfaces.Select((Surfaces a) => a.GetSurfacesWorld()).Collapse().ToArray(), B.surfaces.Select((Surfaces b) => b.GetSurfacesWorld()).Collapse().ToArray(), out overlap, out center);
	}

	public static bool SurfacesConnect(Line2[] surfaces_A, Line2[] surfaces_B, out float overlap, out Vector2 center)
	{
		overlap = 0f;
		center = Vector2.zero;
		foreach (Line2 a in surfaces_A)
		{
			foreach (Line2 b in surfaces_B)
			{
				if (SurfacesConnect(a, b, out overlap, out center))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool SurfacesConnect(Line2 A, Line2 B, out float overlap, out Vector2 center)
	{
		overlap = BiggestOverlap(A, B, out center);
		float num = Vector2.Angle(A.end - A.start, B.end - B.start);
		if (num > 5f && num < 175f)
		{
			return false;
		}
		if (!Intersect(A, B) && ShortestDistance(A, B) > 0.05f)
		{
			return false;
		}
		if (overlap < 0.05f)
		{
			return false;
		}
		return true;
	}

	private static float BiggestOverlap(Line2 A, Line2 B, out Vector2 center)
	{
		float num = 0f;
		Line line = ((A.start.x < A.end.x) ? new Line(A.start.x, A.end.x) : new Line(A.end.x, A.start.x));
		Line line2 = ((B.start.x < B.end.x) ? new Line(B.start.x, B.end.x) : new Line(B.end.x, B.start.x));
		Line obj = ((A.start.y < A.end.y) ? new Line(A.start.y, A.end.y) : new Line(A.end.y, A.start.y));
		Line line3 = ((B.start.y < B.end.y) ? new Line(B.start.y, B.end.y) : new Line(B.end.y, B.start.y));
		float num2 = Mathf.Max(line.start, line2.start);
		float num3 = Mathf.Min(line.end, line2.end);
		if (num2 < num3)
		{
			num = Mathf.Max(num, num3 - num2);
		}
		float num4 = Mathf.Max(obj.start, line3.start);
		float num5 = Mathf.Min(obj.end, line3.end);
		if (num4 < num5)
		{
			num = Mathf.Max(num, num5 - num4);
		}
		center = new Vector2((num2 + num3) * 0.5f, (num4 + num5) * 0.5f);
		return num;
	}

	private static float ShortestDistance(Line2 A, Line2 B)
	{
		float num = float.MaxValue;
		float num2 = DistanceToSegment(A, B.start);
		if (num2 < num)
		{
			num = num2;
		}
		num2 = DistanceToSegment(A, B.end);
		if (num2 < num)
		{
			num = num2;
		}
		num2 = DistanceToSegment(B, A.start);
		if (num2 < num)
		{
			num = num2;
		}
		num2 = DistanceToSegment(B, A.end);
		if (num2 < num)
		{
			num = num2;
		}
		return num;
	}

	private static float DistanceToSegment(Line2 segment, Vector2 point)
	{
		float num = point.x - segment.start.x;
		float num2 = point.y - segment.start.y;
		float num3 = segment.end.x - segment.start.x;
		float num4 = segment.end.y - segment.start.y;
		float num5 = num * num3 + num2 * num4;
		float num6 = num3 * num3 + num4 * num4;
		float num7 = ((num6 != 0f) ? (num5 / num6) : (-1f));
		float num8;
		float num9;
		if (num7 < 0f)
		{
			num8 = segment.start.x;
			num9 = segment.start.y;
		}
		else if (num7 > 1f)
		{
			num8 = segment.end.x;
			num9 = segment.end.y;
		}
		else
		{
			num8 = segment.start.x + num7 * num3;
			num9 = segment.start.y + num7 * num4;
		}
		float num10 = point.x - num8;
		float num11 = point.y - num9;
		return Mathf.Sqrt(num10 * num10 + num11 * num11);
	}

	private static bool Intersect(Line2 A, Line2 B)
	{
		int num = Direction(A.start, A.end, B.start);
		int num2 = Direction(A.start, A.end, B.end);
		int num3 = Direction(B.start, B.end, A.start);
		int num4 = Direction(B.start, B.end, A.end);
		if (num != num2 && num3 != num4)
		{
			return true;
		}
		if (num == 0 && OnLine(A, B.start))
		{
			return true;
		}
		if (num2 == 0 && OnLine(A, B.end))
		{
			return true;
		}
		if (num3 == 0 && OnLine(B, A.start))
		{
			return true;
		}
		if (num4 == 0 && OnLine(B, A.end))
		{
			return true;
		}
		return false;
	}

	private static int Direction(Vector2 start, Vector2 end, Vector2 point)
	{
		float num = (end.y - start.y) * (point.x - end.x) - (end.x - start.x) * (point.y - end.y);
		if (num == 0f)
		{
			return 0;
		}
		if (num < 0f)
		{
			return 2;
		}
		return 1;
	}

	private static bool OnLine(Line2 line, Vector2 point)
	{
		if (point.x <= Mathf.Max(line.start.x, line.end.x) && point.x <= Mathf.Min(line.start.x, line.end.x) && point.y <= Mathf.Max(line.start.y, line.end.y))
		{
			return point.y <= Mathf.Min(line.start.y, line.end.y);
		}
		return false;
	}
}
