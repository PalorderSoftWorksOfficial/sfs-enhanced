using System;
using UnityEngine;

[Serializable]
public struct Color2(Color start, Color end)
{
	public Color start = start;

	public Color end = end;

	public Color Lerp(float t)
	{
		return Color.Lerp(start, end, t);
	}
}
