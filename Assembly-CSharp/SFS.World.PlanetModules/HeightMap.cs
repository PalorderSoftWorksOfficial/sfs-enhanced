using System;
using UnityEngine;

namespace SFS.World.PlanetModules;

[Serializable]
public class HeightMap
{
	public float[] points = new float[2] { 0f, 1f };

	public HeightMap()
	{
	}

	public HeightMap(float[] points)
	{
		this.points = points;
	}

	public HeightMap(Texture2D image)
	{
		Texture2D image2 = image;
		base._002Ector();
		points = new float[image2.width];
		for (int i = 0; i < points.Length; i++)
		{
			points[points.Length - i - 1] = GetHeightAtX(i);
		}
		float GetHeightAtX(int x)
		{
			for (int j = 0; j < image2.height; j++)
			{
				float a = image2.GetPixel(x, j).a;
				if (a < 1f)
				{
					return ((float)j + a) / (float)image2.height;
				}
			}
			return 1f;
		}
	}

	public double EvaluateDoubleOut(double a)
	{
		int num = points.Length - 1;
		a *= (double)num;
		int num2 = (int)a % num;
		float num3 = (float)(a % 1.0);
		return points[num2] * (1f - num3) + points[num2 + 1] * num3;
	}

	public float EvaluateClamped(float a)
	{
		if (a >= 1f)
		{
			float[] array = points;
			return array[array.Length - 1];
		}
		if (a <= 0f)
		{
			return points[0];
		}
		return (float)EvaluateDoubleOut(a);
	}
}
