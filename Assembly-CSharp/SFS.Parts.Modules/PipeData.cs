using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public abstract class PipeData : PolygonData
{
	public float depthMultiplier = 1f;

	[Space]
	public bool advancedCut;

	[Range(-1.99f, 1.99f)]
	public float cut;

	public AdvancedCut advancedCutData;

	[Space]
	public bool reduceResolution = true;

	public Pipe pipe;

	protected void SetData(Pipe pipe)
	{
		this.pipe = pipe;
		List<PipePoint> points = pipe.points;
		Vector2[] array = new Vector2[points.Count * 2];
		for (int i = 0; i < points.Count; i++)
		{
			PipePoint pipePoint = points[i];
			pipePoint.cutLeft = (advancedCut ? advancedCutData.cuts[(advancedCutData.cuts.Length != 1) ? i : 0].left : (Mathf.Clamp(cut - 1f, -1f, 1f) / 2f + 0.5f));
			pipePoint.cutRight = (advancedCut ? advancedCutData.cuts[(advancedCutData.cuts.Length != 1) ? i : 0].right : (Mathf.Clamp(cut + 1f, -1f, 1f) / 2f + 0.5f));
			array[i] = pipePoint.GetPosition(pipePoint.cutLeft * 2f - 1f);
			array[array.Length - 1 - i] = pipePoint.GetPosition(pipePoint.cutRight * 2f - 1f);
		}
		if (reduceResolution && array.Length > 4)
		{
			SetData(new Polygon(array), new Polygon(ToFastPoints(array, 0.05f)));
		}
		else
		{
			SetData(new Polygon(array));
		}
	}

	private static Vector2[] ToFastPoints(Vector2[] points, float tolerance)
	{
		if (points.Length < 3)
		{
			return points;
		}
		int num = points.Length - 1;
		List<int> pointIndexesToKeep = new List<int> { 0, num };
		while (points[0] == points[num])
		{
			num--;
		}
		ToFastPoints(points, 0, num, tolerance, ref pointIndexesToKeep);
		pointIndexesToKeep.Sort();
		Vector2[] array = new Vector2[pointIndexesToKeep.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = points[pointIndexesToKeep[i]];
		}
		return array;
	}

	private static void ToFastPoints(Vector2[] points, int firstPoint, int lastPoint, float tolerance, ref List<int> pointIndexesToKeep)
	{
		float num = 0f;
		int num2 = 0;
		for (int i = firstPoint; i < lastPoint; i++)
		{
			float distanceSqrt = GetDistanceSqrt(points[firstPoint], points[lastPoint], points[i]);
			if (distanceSqrt > num)
			{
				num = distanceSqrt;
				num2 = i;
			}
		}
		if (num > tolerance && num2 != 0)
		{
			pointIndexesToKeep.Add(num2);
			ToFastPoints(points, firstPoint, num2, tolerance, ref pointIndexesToKeep);
			ToFastPoints(points, num2, lastPoint, tolerance, ref pointIndexesToKeep);
		}
	}

	private static float GetDistanceSqrt(Vector2 point_A, Vector2 point_B, Vector2 point)
	{
		Vector2 vector = point_B - point_A;
		float num = ((point.x - point_A.x) * vector.x + (point.y - point_A.y) * vector.y) / (vector.x * vector.x + vector.y * vector.y);
		if (num < 0f)
		{
			return (point - point_A).sqrMagnitude;
		}
		if (num > 1f)
		{
			return (point - point_B).sqrMagnitude;
		}
		return (point - (point_A + vector * num)).sqrMagnitude;
	}

	public override bool Raycast(Vector2 point, out float depth)
	{
		for (int i = 0; i < pipe.points.Count - 1; i++)
		{
			if (Depth(pipe.points[i], pipe.points[i + 1], point, out var depth2))
			{
				depth = base.BaseDepth + depth2 * depthMultiplier;
				return true;
			}
		}
		depth = 0f;
		return false;
	}

	private static bool Depth(PipePoint p0, PipePoint p1, Vector2 point, out float depth)
	{
		return Depth(p1.position, p1.width, p0.position, p0.width, point, out depth);
	}

	private static bool Depth(Vector2 bottom, Vector2 bottomWidth, Vector2 top, Vector2 topWidth, Vector2 point, out float depth)
	{
		depth = 0f;
		Vector2 vector = bottom - bottomWidth / 2f;
		Vector2 vector2 = top - topWidth / 2f;
		Vector2 vector3 = top + topWidth / 2f;
		Vector2 vector4 = bottom + bottomWidth / 2f;
		if (new Polygon(vector, vector2, vector3, vector4).convexPolygons[0].GetDistanceToPolygon(point) > 0f)
		{
			return false;
		}
		Vector2 vector5 = UV_Utility.Get_UV_InQuad(point, vector, vector2, vector3, vector4);
		vector5.x = ((vector5.x > 0.5f) ? ((1f - vector5.x) * 2f) : (vector5.x * 2f));
		depth = Mathf.Lerp(0f, Mathf.Lerp(bottomWidth.x / 2f, topWidth.x / 2f, 1f - vector5.y), vector5.x);
		return true;
	}
}
