using System;
using UnityEngine;

public static class Math_Utility
{
	public static int GetFitsTime(double value, double a, double b)
	{
		return (int)((b - a) / value);
	}

	public static bool IsInsideRange(double value, double a, double b)
	{
		if (value >= Math.Min(a, b))
		{
			return value <= Math.Max(a, b);
		}
		return false;
	}

	public static bool InArea(Rect area, Vector2 position, float extra)
	{
		Vector2 vector = area.min - Vector2.one * extra;
		Vector2 vector2 = area.max + Vector2.one * extra;
		if (position.x > vector.x && position.y > vector.y)
		{
			if (position.x < vector2.x)
			{
				return position.y < vector2.y;
			}
			return false;
		}
		return false;
	}

	public static double Floor_ForNavigation(double floor, double level, double a)
	{
		if (a < floor)
		{
			return floor;
		}
		return floor + Math.Floor((a - floor) / level) * level;
	}

	public static double Clamp(double value, double min, double max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static double Clamp01(double value)
	{
		if (value < 0.0)
		{
			return 0.0;
		}
		if (value > 1.0)
		{
			return 1.0;
		}
		return value;
	}

	public static double Lerp(double a, double b, double t)
	{
		return (1.0 - t) * a + t * b;
	}

	public static double InverseLerp(double a, double b, double value)
	{
		if (a == b)
		{
			return 0.0;
		}
		return (value - a) / (b - a);
	}

	public static float InverseLerpUnclamped(float a, float b, float value)
	{
		return (value - a) / (b - a);
	}

	public static void GetRectOverreach(Rect A, Rect area, bool clampNegative, out Vector2 leftDown, out Vector2 rightUp)
	{
		leftDown = area.min - A.min;
		rightUp = A.max - area.max;
		if (clampNegative)
		{
			leftDown = new Vector2(Mathf.Max(0f, leftDown.x), Mathf.Max(0f, leftDown.y));
			rightUp = new Vector2(Mathf.Max(0f, rightUp.x), Mathf.Max(0f, rightUp.y));
		}
	}

	public static float Round(this float A, float range)
	{
		return Mathf.Round(A / range) * range;
	}

	public static float Round_Anchor(this float A, float range, float anchor)
	{
		return Mathf.Round((A - anchor) / range) * range + anchor;
	}

	public static double Round(this double A, double range)
	{
		return Math.Round(A / range) * range;
	}

	public static Vector2 Round(this Vector2 A, float range)
	{
		return new Vector2(Mathf.Round(A.x / range) * range, Mathf.Round(A.y / range) * range);
	}

	public static Vector2 Round(this Vector2 A, Vector2 range)
	{
		return new Vector2(Mathf.Round(A.x / range.x) * range.y, Mathf.Round(A.y / range.y) * range.y);
	}

	public static float AngleDegrees(this Vector2 a)
	{
		return a.AngleRadians() * 57.29578f;
	}

	public static float AngleRadians(this Vector2 a)
	{
		return Mathf.Atan2(a.y, a.x);
	}

	public static Vector2 GetLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out bool parallel)
	{
		Vector2 vector = a2 - a1;
		Vector2 vector2 = b2 - b1;
		float num = vector.y * vector2.x - vector.x * vector2.y;
		parallel = num == 0f;
		if (parallel)
		{
			return default(Vector2);
		}
		float num2 = ((a1.x - b1.x) * vector2.y + (b1.y - a1.y) * vector2.x) / num;
		return new Vector2(a1.x + vector.x * num2, a1.y + vector.y * num2);
	}

	public static float GetClosestPointOnLine(Vector2 p1, Vector2 p2, Vector2 point)
	{
		Vector2 rhs = p2 - p1;
		return Vector2.Dot(point - p1, rhs) / rhs.sqrMagnitude;
	}

	public static double GetClosestPointOnLine(Double2 p1, Double2 p2, Double2 point)
	{
		Double2 b = p2 - p1;
		return Double2.Dot(point - p1, b) / b.sqrMagnitude;
	}

	public static bool IsClockwise(Vector2 a, Vector2 b, Vector2 c)
	{
		return (b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y) >= 0f;
	}

	public static Vector3 ToVector3(this Vector2 a, float z)
	{
		return new Vector3(a.x, a.y, z);
	}

	public static float DegreesToRadians(float degrees)
	{
		return degrees * ((float)Math.PI / 180f);
	}

	public static double DegreesToRadians(double degrees)
	{
		return degrees * (Math.PI / 180.0);
	}

	public static double NormalizeAngleDegrees(double angle)
	{
		while (angle > 180.0)
		{
			angle -= 360.0;
		}
		while (angle < -180.0)
		{
			angle += 360.0;
		}
		return angle;
	}

	public static double NormalizePositiveAngleDegrees(double angle)
	{
		while (angle > 360.0)
		{
			angle -= 360.0;
		}
		while (angle < 0.0)
		{
			angle += 360.0;
		}
		return angle;
	}

	public static double NormalizePositiveAngleRadians(double angle)
	{
		while (angle > Math.PI)
		{
			angle -= Math.PI;
		}
		while (angle < 0.0)
		{
			angle += Math.PI;
		}
		return angle;
	}
}
