using System;
using System.Linq;
using UnityEngine;

namespace SFS.World.PlanetModules;

[Serializable]
public class PostProcessingModule
{
	[Serializable]
	public class Key
	{
		public float height;

		public float shadowIntensity = 1.75f;

		public float starIntensity = 1f;

		public float hueShift;

		public float saturation = 1f;

		public float contrast = 1.1f;

		public float red = 1f;

		public float green = 1f;

		public float blue = 1f;

		public Key()
		{
		}

		public Key(float height, float shadowIntensity, float starIntensity, float hueShift, float saturation, float contrast, float red, float green, float blue)
		{
			this.height = height;
			this.shadowIntensity = shadowIntensity;
			this.starIntensity = starIntensity;
			this.hueShift = hueShift;
			this.saturation = saturation;
			this.contrast = contrast;
			this.red = red;
			this.green = green;
			this.blue = blue;
		}

		public static Key Lerp(Key a, Key b, float t)
		{
			return new Key(Mathf.Lerp(a.height, b.height, t), Mathf.Lerp(a.shadowIntensity, b.shadowIntensity, t), Mathf.Lerp(a.starIntensity, b.starIntensity, t), Mathf.Lerp(a.hueShift, b.hueShift, t), Mathf.Lerp(a.saturation, b.saturation, t), Mathf.Lerp(a.contrast, b.contrast, t), Mathf.Lerp(a.red, b.red, t), Mathf.Lerp(a.green, b.green, t), Mathf.Lerp(a.blue, b.blue, t));
		}
	}

	public Key[] keys;

	public Key Evaluate(float height)
	{
		if (height >= keys.Last().height)
		{
			return keys.Last();
		}
		if (height <= keys.First().height)
		{
			return keys.First();
		}
		int i;
		for (i = 0; height > keys[i + 1].height; i++)
		{
		}
		return Key.Lerp(keys[i], keys[i + 1], Mathf.InverseLerp(keys[i].height, keys[i + 1].height, height));
	}
}
