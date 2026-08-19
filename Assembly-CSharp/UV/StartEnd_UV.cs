using SFS.Parts;
using UnityEngine;

namespace UV;

public class StartEnd_UV
{
	public Line height;

	public Line vertical_UV;

	public Line2 texture_UV;

	public Texture2D texture;

	public PartTexture data;

	public float fixedWidthValue;

	public StartEnd_UV(Line height, Line vertical_UV, Line2 texture_UV, Texture2D texture, PartTexture data, float fixedWidthValue)
	{
		this.height = height;
		this.vertical_UV = vertical_UV;
		this.texture_UV = texture_UV;
		this.texture = texture;
		this.data = data;
		this.fixedWidthValue = fixedWidthValue;
	}

	public StartEnd_UV Cut(Line cut)
	{
		Line line = new Line(height.InverseLerp(cut.start), height.InverseLerp(cut.end));
		return new StartEnd_UV(vertical_UV: new Line(vertical_UV.Lerp(line.start), vertical_UV.Lerp(line.end)), height: cut, texture_UV: texture_UV, texture: texture, data: data, fixedWidthValue: fixedWidthValue);
	}
}
