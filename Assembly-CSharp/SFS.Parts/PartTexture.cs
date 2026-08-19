using System;
using System.Collections.Generic;
using SFS.Parts.Modules;
using UnityEngine;
using UnityEngine.Serialization;
using UV;

namespace SFS.Parts;

[Serializable]
public class PartTexture
{
	public PerValueTexture[] textures;

	[Space]
	public BorderData border_Bottom;

	public BorderData border_Top;

	[Space]
	public CenterData center;

	[Space]
	public bool fixedWidth;

	[FormerlySerializedAs("width")]
	public float fixedWidthValue = 1f;

	[Space]
	public bool flipToLight_X = true;

	public bool flipToLight_Y = true;

	[Space]
	public bool metalTexture;

	[Space]
	public Sprite icon;

	public List<StartEnd_UV> Get_UV(Pipe shape, Line segment, float shapeWidth, Transform meshHolder, Vector2 lightDirection)
	{
		if (shape.points.Count < 2)
		{
			return new List<StartEnd_UV>();
		}
		Texture2D texture = GetBestTexture(shapeWidth);
		Line2 uv = Line2.StartSize(Vector2.zero, Vector2.one);
		if (flipToLight_X && Vector2.Angle(meshHolder.TransformVector(Vector2.left), lightDirection) > 90f)
		{
			uv.FlipHorizontally();
		}
		if (flipToLight_Y && Vector2.Angle(meshHolder.TransformVector(Vector2.up), lightDirection) > 90f)
		{
			uv.FlipVertically();
		}
		Line cut = new Line(segment.start, segment.start + GetBottomBorderSize(shape, segment, texture));
		Line cut2 = new Line(segment.end - GetTopBorderSize(shape, segment, texture), segment.end);
		Line line = new Line(cut.end, cut2.start);
		List<StartEnd_UV> output = new List<StartEnd_UV>();
		AddQuad(cut, new Line(0f, GetCenterUV().start), uv, texture, this, fixedWidthValue);
		if (center.mode == CenterData.CenterMode.Stretch)
		{
			AddQuad(line, GetCenterUV(), uv, texture, this, fixedWidthValue);
		}
		else if (center.mode == CenterData.CenterMode.Logo)
		{
			float num = GetCenterSize(line.Lerp(center.logoHeightPercent));
			float num2 = fixedWidthValue;
			if (center.scaleLogoToFit && num > line.Size * 0.85f)
			{
				float num3 = Mathf.Max(num * 0.75f, line.Size * 0.85f);
				num2 *= num3 / num;
				num = num3;
			}
			Line cut3 = Line.StartSize(Mathf.Lerp(line.start, line.end - num, center.logoHeightPercent), num);
			AddQuad(new Line(line.start, cut3.start), Line.CenterSize(GetCenterUV().start, 0f), uv, texture, this, fixedWidthValue);
			AddQuadClamped(cut3, GetCenterUV(), uv, line, texture, this, num2);
			AddQuad(new Line(cut3.end, line.end), Line.CenterSize(GetCenterUV().end, 0f), uv, texture, this, fixedWidthValue);
		}
		else if (center.mode == CenterData.CenterMode.Tile)
		{
			float num4 = GetCenterSize(line.start);
			for (int i = 0; (float)i < Mathf.Ceil(line.Size / num4); i++)
			{
				AddQuadClamped(Line.StartSize(line.start + num4 * (float)i, num4), GetCenterUV(), uv, line, texture, this, fixedWidthValue);
			}
		}
		AddQuad(cut2, new Line(GetCenterUV().end, 1f), uv, texture, this, fixedWidthValue);
		return output;
		void AddQuad(Line height, Line vertical_UV, Line2 texture_UV, Texture2D tex, PartTexture data, float fixedWidthValue)
		{
			if (height.Size > 0f)
			{
				output.Add(new StartEnd_UV(height, vertical_UV, texture_UV, tex, data, fixedWidthValue));
			}
		}
		void AddQuadClamped(Line line2, Line vertical_UV, Line2 uv2, Line clamp, Texture2D tex, PartTexture data, float fixedWidthValue)
		{
			Line cut4 = new Line(Mathf.Clamp(line2.start, clamp.start, clamp.end), Mathf.Clamp(line2.end, clamp.start, clamp.end));
			Line vertical_UV2 = new Line(vertical_UV.Lerp(Mathf.InverseLerp(line2.start, line2.end, cut4.start)), vertical_UV.Lerp(Mathf.InverseLerp(line2.start, line2.end, cut4.end)));
			AddQuad(cut4, vertical_UV2, uv2, tex, data, fixedWidthValue);
		}
		float GetCenterSize(float height)
		{
			if (center.sizeMode != VerticalSizeMode.Aspect)
			{
				return center.size;
			}
			return GetCenterUV().Size * GetAspectRatio(texture) * (fixedWidth ? fixedWidthValue : shape.GetWidthAtHeight(height).magnitude);
		}
	}

	public Texture2D GetBestTexture(float shapeWidth)
	{
		PerValueTexture perValueTexture = textures[0];
		PerValueTexture[] array = textures;
		foreach (PerValueTexture perValueTexture2 in array)
		{
			if (Mathf.Abs(perValueTexture2.ideal - shapeWidth) <= Mathf.Abs(perValueTexture.ideal - shapeWidth))
			{
				perValueTexture = perValueTexture2;
			}
		}
		return perValueTexture.texture;
	}

	private float GetBottomBorderSize(Pipe shape, Line segment, Texture texture)
	{
		if (border_Bottom.uvSize == 0f)
		{
			return 0f;
		}
		return Mathf.Min((border_Bottom.sizeMode == VerticalSizeMode.Fixed) ? border_Bottom.size : (border_Bottom.uvSize * GetAspectRatio(texture) * shape.GetWidthAtHeight(segment.start).magnitude), segment.Size / 2f);
	}

	private float GetTopBorderSize(Pipe shape, Line segment, Texture texture)
	{
		if (border_Top.uvSize == 0f)
		{
			return 0f;
		}
		return Mathf.Min((border_Top.sizeMode == VerticalSizeMode.Fixed) ? border_Top.size : (border_Top.uvSize * GetAspectRatio(texture) * shape.GetWidthAtHeight(segment.end).magnitude), segment.Size / 2f);
	}

	private float GetAspectRatio(Texture texture)
	{
		return (float)texture.height / (float)texture.width;
	}

	private Line GetCenterUV()
	{
		return new Line(border_Bottom.uvSize, 1f - border_Top.uvSize);
	}
}
