using System;
using UnityEngine;

[Serializable]
public struct Line
{
	public float start;

	public float end;

	public float Size => end - start;

	public float Center => (start + end) / 2f;

	public Line(float start, float end)
	{
		this.start = start;
		this.end = end;
	}

	public static Line StartSize(float start, float size)
	{
		return new Line(start, start + size);
	}

	public static Line CenterSize(float center, float size)
	{
		return new Line(center - size / 2f, center + size / 2f);
	}

	public void Set(float start, float end)
	{
		this.start = start;
		this.end = end;
	}

	public float Lerp(float t)
	{
		return Mathf.Lerp(start, end, t);
	}

	public float InverseLerp(float a)
	{
		return Mathf.InverseLerp(start, end, a);
	}
}
