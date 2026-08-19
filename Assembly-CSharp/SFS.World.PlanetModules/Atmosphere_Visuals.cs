using System;
using UnityEngine;

namespace SFS.World.PlanetModules;

[Serializable]
public class Atmosphere_Visuals
{
	[Serializable]
	public class Gradient
	{
		public int positionZ = -1;

		public double height = 1.0;

		public string texture = "None";
	}

	[Serializable]
	public class Clouds
	{
		public string texture = "None";

		public float startHeight = 1f;

		public float width = 1f;

		public float height = 1f;

		public float alpha = 1f;

		public float velocity;
	}

	[Serializable]
	public class ColorGradient
	{
		[Serializable]
		public class Key
		{
			public Color color;

			public float distance;

			public Key(Color color, float distance)
			{
				this.color = color;
				this.distance = distance;
			}
		}

		public Key[] keys;

		public Color Evaluate(float distance)
		{
			if (keys == null || keys.Length == 0)
			{
				return Color.clear;
			}
			if (distance <= keys[0].distance)
			{
				return keys[0].color;
			}
			if (distance >= keys[keys.Length - 1].distance)
			{
				return keys[keys.Length - 1].color;
			}
			int i;
			for (i = 0; distance > keys[i + 1].distance; i++)
			{
			}
			return Color.Lerp(keys[i].color, keys[i + 1].color, Mathf.InverseLerp(keys[i].distance, keys[i + 1].distance, distance));
		}
	}

	public Gradient GRADIENT;

	public Clouds CLOUDS;

	public ColorGradient FOG;
}
