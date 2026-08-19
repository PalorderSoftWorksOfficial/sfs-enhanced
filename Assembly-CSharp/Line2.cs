using System;
using UnityEngine;

[Serializable]
public struct Line2
{
	public Vector2 start;

	public Vector2 end;

	public Vector2 Size => end - start;

	public float SizeX => end.x - start.x;

	public float SizeY => end.y - start.y;

	public Line2(Vector2 start, Vector2 end)
	{
		this.start = start;
		this.end = end;
	}

	public static Line2 StartSize(Vector2 start, Vector2 size)
	{
		return new Line2(start, start + size);
	}

	public Vector2 Lerp(float t)
	{
		return Vector2.Lerp(start, end, t);
	}

	public Vector2 Lerp(float t_X, float t_Y)
	{
		return new Vector2(Mathf.Lerp(start.x, end.x, t_X), Mathf.Lerp(start.y, end.y, t_Y));
	}

	public Vector2 LerpUnclamped(float t)
	{
		return new Vector2(Mathf.LerpUnclamped(start.x, end.x, t), Mathf.LerpUnclamped(start.y, end.y, t));
	}

	public Vector2 LerpUnclamped(float t_X, float t_Y)
	{
		return new Vector2(Mathf.LerpUnclamped(start.x, end.x, t_X), Mathf.LerpUnclamped(start.y, end.y, t_Y));
	}

	public void Flip()
	{
		Vector2 vector = end;
		Vector2 vector2 = start;
		start = vector;
		end = vector2;
	}

	public void FlipHorizontally()
	{
		ref float x = ref start.x;
		ref float x2 = ref end.x;
		float x3 = end.x;
		float x4 = start.x;
		x = x3;
		x2 = x4;
	}

	public void FlipVertically()
	{
		ref float y = ref start.y;
		ref float y2 = ref end.y;
		float y3 = end.y;
		float y4 = start.y;
		y = y3;
		y2 = y4;
	}

	public Vector2 GetPositionAtX(float x)
	{
		float num = (x - start.x) / (end.x - start.x);
		if (num <= 0f)
		{
			return start;
		}
		if (num >= 1f)
		{
			return end;
		}
		return new Vector2(x, GetHeightAtX_Unclamped(x));
	}

	public Vector2 GetPositionAtY(float y)
	{
		float num = (y - start.y) / (end.y - start.y);
		if (num <= 0f)
		{
			return start;
		}
		if (num >= 1f)
		{
			return end;
		}
		return start + (end - start) * num;
	}

	public Vector2 GetPositionAtX_Unclamped(float x)
	{
		return new Vector2(x, GetHeightAtX_Unclamped(x));
	}

	public float GetHeightAtX(float x)
	{
		float num = (x - start.x) / (end.x - start.x);
		if (num <= 0f)
		{
			return start.y;
		}
		if (num >= 1f)
		{
			return end.y;
		}
		return start.y + (end.y - start.y) * num;
	}

	public float GetHeightAtX_Unclamped(float x)
	{
		float num = (x - start.x) / (end.x - start.x);
		return start.y + (end.y - start.y) * num;
	}

	public float GetSlope()
	{
		if (start.x == end.x)
		{
			Debug.LogError("Width 0");
			return 0f;
		}
		return (end.y - start.y) / (end.x - start.x);
	}

	public static float GetSlope_Abs(Vector2 start, Vector2 end)
	{
		return Mathf.Abs((end.y - start.y) / (end.x - start.x));
	}

	public static float GetSlope(Vector2 start, Vector2 end)
	{
		return (end.y - start.y) / (end.x - start.x);
	}

	public static bool FindIntersection_Unclamped(Line2 a, Line2 b, out Vector2 position)
	{
		float num = a.end.x - a.start.x;
		float num2 = a.end.y - a.start.y;
		float num3 = b.end.x - b.start.x;
		float num4 = b.end.y - b.start.y;
		float num5 = num2 * num3 - num * num4;
		if (num5 == 0f)
		{
			position = Vector2.zero;
			return false;
		}
		float num6 = ((a.start.x - b.start.x) * num4 + (b.start.y - a.start.y) * num3) / num5;
		position = new Vector2(a.start.x + num * num6, a.start.y + num2 * num6);
		return true;
	}
}
