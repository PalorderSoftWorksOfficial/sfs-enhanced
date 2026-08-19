using UnityEngine;

namespace SFS;

public static class UV_Utility
{
	public static Vector2 UV(Vector2 point, Vector2[] bounds)
	{
		float x = Get_UV_InQuad(point, bounds[1], bounds[0], bounds[3], bounds[2]).x;
		float y = Get_UV_InQuad(point, bounds[0], bounds[1], bounds[2], bounds[3]).y;
		return new Vector2(x, 1f - y);
	}

	public static Vector2 Get_UV_InQuad(Vector2 point, Vector2 p1, Vector2 p4, Vector2 p3, Vector2 p2)
	{
		return UV(p1.x, p1.y, p2.x, p2.y, p3.x, p3.y, p4.x, p4.y, point.x, point.y);
	}

	private static Vector2 UV(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4, float x, float y)
	{
		float num = (x3 - x4) * (y1 - y2) - (x1 - x2) * (y3 - y4);
		float num2 = (x1 - x4) * (y2 - y3) - (x2 - x3) * (y1 - y4);
		float num3 = -1f / (2f * num);
		float num4 = (0f - x2) * y + x3 * y - x4 * y - x * y1 - x3 * y1 + 2f * x4 * y1 + x * y2 - x4 * y2 - x * y3 + x1 * (y + y3 - 2f * y4) + x * y4 + x2 * y4;
		float num5 = Mathf.Sqrt(4f * num * (x4 * (0f - y + y1) + x1 * (y - y4) + x * (0f - y1 + y4)) + Mathf.Pow(x3 * y - x4 * y - x3 * y1 + 2f * x4 * y1 - x4 * y2 + x1 * (y + y3 - 2f * y4) + x2 * (0f - y + y4) + x * (0f - y1 + y2 - y3 + y4), 2f));
		float num6 = 1f / (2f * num2);
		float num7 = x2 * y - x3 * y + x4 * y + x * y1 - 2f * x2 * y1 + x3 * y1 - x * y2 - x4 * y2 + x * y3 - x1 * (y - 2f * y2 + y3) - x * y4 + x2 * y4;
		float num8 = num3 * (num4 + num5);
		float num9 = 1f - num6 * (num7 + num5);
		if (num == 0f && num2 == 0f)
		{
			num8 = Math_Utility.InverseLerpUnclamped(x1, x2, x);
			num9 = Math_Utility.InverseLerpUnclamped(y4, y1, y);
		}
		else if (num == 0f)
		{
			Vector2 vector = Vector2.Lerp(new Vector2(x4, y4), new Vector2(x1, y1), num9);
			Vector2 vector2 = Vector2.Lerp(new Vector2(x3, y3), new Vector2(x2, y2), num9);
			num8 = Math_Utility.InverseLerpUnclamped(vector.x, vector2.x, x);
		}
		else if (num2 == 0f)
		{
			Vector2 vector3 = Vector2.Lerp(new Vector2(x4, y4), new Vector2(x3, y3), num8);
			Vector2 vector4 = Vector2.Lerp(new Vector2(x1, y1), new Vector2(x2, y2), num8);
			num9 = Math_Utility.InverseLerpUnclamped(vector3.y, vector4.y, y);
		}
		return new Vector2(num8, num9);
	}

	public static float[] GetQuadM(Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
	{
		Vector2 lineIntersection = Math_Utility.GetLineIntersection(topLeft, bottomRight, topRight, bottomLeft, out var parallel);
		if (!parallel)
		{
			float num = Vector2.Distance(topLeft, lineIntersection);
			float num2 = Vector2.Distance(topRight, lineIntersection);
			float num3 = Vector2.Distance(bottomRight, lineIntersection);
			float num4 = Vector2.Distance(bottomLeft, lineIntersection);
			float num5 = num + num3;
			float num6 = num2 + num4;
			return new float[4]
			{
				num5 / num3,
				num6 / num4,
				num5 / num,
				num6 / num2
			};
		}
		return new float[4] { 1f, 1f, 1f, 1f };
	}
}
