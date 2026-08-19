using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Variables;
using UnityEngine;
using UV;

namespace SFS.Parts.Modules;

[Serializable]
public class Textures
{
	[Serializable]
	public class TextureKey
	{
		public TextureSelector texture;

		public float height;
	}

	[Serializable]
	public class TextureSelector
	{
		public ColorTexture colorTexture;

		public ShapeTexture shapeTexture;
	}

	public enum WidthMode
	{
		Standard,
		Composed
	}

	public Mode textureMode;

	public TextureSelector texture;

	public TextureKey[] textures;

	[Space]
	public WidthMode widthMode;

	public Composed_Float width;

	public List<StartEnd_UV>[] GetOutput(Pipe shape, Transform meshHolder, Vector2 lightDirection)
	{
		float shapeWidth = GetShapeWidth(shape);
		if (textureMode == Mode.Single)
		{
			Line segment = new Line(0f, shape.points.Last().height);
			return new List<StartEnd_UV>[3]
			{
				texture.colorTexture.Get_UV(shape, segment, shapeWidth, meshHolder, lightDirection),
				texture.shapeTexture.Get_UV(shape, segment, shapeWidth, meshHolder, lightDirection),
				texture.shapeTexture.shadowTex.Get_UV(shape, segment, shapeWidth, meshHolder, lightDirection)
			};
		}
		List<StartEnd_UV>[] array = new List<StartEnd_UV>[3]
		{
			new List<StartEnd_UV>(),
			new List<StartEnd_UV>(),
			new List<StartEnd_UV>()
		};
		for (int i = 0; i < textures.Length; i++)
		{
			Line segment2 = new Line(ConvertHeight((i > 0) ? textures[i - 1].height : 0f), ConvertHeight(textures[i].height));
			array[0].AddRange(textures[i].texture.colorTexture.Get_UV(shape, segment2, shapeWidth, meshHolder, lightDirection));
			array[1].AddRange(textures[i].texture.shapeTexture.Get_UV(shape, segment2, shapeWidth, meshHolder, lightDirection));
			array[2].AddRange(textures[i].texture.shapeTexture.shadowTex.Get_UV(shape, segment2, shapeWidth, meshHolder, lightDirection));
		}
		return array;
		float ConvertHeight(float height)
		{
			if (!(height >= 0f))
			{
				return shape.points.Last().height + height;
			}
			return height;
		}
	}

	public float GetShapeWidth(Pipe shape)
	{
		if (widthMode == WidthMode.Standard)
		{
			float magnitude = shape.points.First().width.magnitude;
			float magnitude2 = shape.points.Last().width.magnitude;
			float num = (magnitude + magnitude2) * 0.5f;
			float num2 = Mathf.Max(magnitude, magnitude2);
			return (num + num2) * 0.5f;
		}
		if (widthMode == WidthMode.Composed)
		{
			return width.Value;
		}
		throw new Exception("Width mode is not supported: " + widthMode);
	}
}
