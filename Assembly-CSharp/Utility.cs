using UnityEngine;

public static class Utility
{
	public static float GetDeltaV(float isp, float fullMass, float dryMass)
	{
		return isp * 9.8f * Mathf.Log(fullMass / dryMass);
	}

	public static Vector2 Rotate_Radians(this Vector2 a, float b)
	{
		float num = Mathf.Cos(b);
		float num2 = Mathf.Sin(b);
		return new Vector2(a.x * num - a.y * num2, a.x * num2 + a.y * num);
	}

	public static Vector2 Rotate_90(this Vector2 a)
	{
		return new Vector2(0f - a.y, a.x);
	}

	public static void DrawGizmosBox(Vector2 min, Vector2 max, Color color, bool border)
	{
		Gizmos.color = color;
		Gizmos.DrawCube((min + max) / 2f, max - min);
		if (border)
		{
			Debug.DrawLine(min, new Vector2(min.x, max.y), color);
			Debug.DrawLine(min, new Vector2(max.x, min.y), color);
			Debug.DrawLine(max, new Vector2(min.x, max.y), color);
			Debug.DrawLine(max, new Vector2(max.x, min.y), color);
		}
	}

	public static void DrawArrow_Ray(Vector2 position, Vector2 size, Color c, float sizeM = 1f)
	{
		Debug.DrawRay(position, size, c);
		Vector2 vector = size.normalized * 0.15f;
		Debug.DrawRay(position + size, vector * (-0.1f * sizeM) - vector.Rotate_90() * (0.05f * sizeM), c);
		Debug.DrawRay(position + size, vector * (-0.1f * sizeM) + vector.Rotate_90() * (0.05f * sizeM), c);
	}

	public static void DrawArrow(Vector2 a, Vector2 b, Color c, float sizeM = 1f)
	{
		DrawArrow_Ray(a, b - a, c, sizeM);
	}
}
