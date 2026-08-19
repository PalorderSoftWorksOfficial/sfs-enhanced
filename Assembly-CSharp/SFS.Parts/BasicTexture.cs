using System;
using UnityEngine;

namespace SFS.Parts;

[CreateAssetMenu]
public class BasicTexture : ScriptableObject
{
	[Serializable]
	public class Layer
	{
		public Texture2D texture;

		public bool flipToLight_X = true;

		public bool flipToLight_Y = true;

		public Line2 Get_Rect(Transform holder)
		{
			Line2 result = Line2.StartSize(Vector2.zero, Vector2.one);
			if (flipToLight_X && Vector2.Angle(holder.TransformVector(Vector2.left), new Vector2(-1f, 1f)) > 90f)
			{
				result.FlipHorizontally();
			}
			if (flipToLight_Y && Vector2.Angle(holder.TransformVector(Vector2.up), new Vector2(-1f, 1f)) > 90f)
			{
				result.FlipVertically();
			}
			return result;
		}
	}

	public Layer colorTex;

	public Layer shapeTex;

	public Layer shadowTex;
}
