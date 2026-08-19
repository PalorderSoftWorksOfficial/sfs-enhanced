using System.Collections.Generic;
using SFS.Parts.Modules;
using UnityEngine;
using UV;

namespace SFS.Parts;

public abstract class TextureAssetBase : ScriptableObject
{
	public bool multiple;

	[Space]
	public Segment[] segments;

	protected abstract PartTexture Texture { get; }

	public List<StartEnd_UV> Get_UV(Pipe shape, Line segment, float shapeWidth, Transform meshHolder, Vector2 lightDirection)
	{
		if (multiple)
		{
			List<StartEnd_UV> list = new List<StartEnd_UV>();
			float start = segment.start;
			Segment[] array = segments;
			foreach (Segment segment2 in array)
			{
				float num = ((segment2.height > 0f) ? (segment.start + segment2.height) : (segment.end + segment2.height));
				Line segment3 = new Line(start, num);
				start = num;
				list.AddRange(segment2.texture.Get_UV(shape, segment3, shapeWidth, meshHolder, lightDirection));
			}
			return list;
		}
		return Texture.Get_UV(shape, segment, shapeWidth, meshHolder, lightDirection);
	}

	public PartTexture GetTexID()
	{
		if (!multiple)
		{
			return Texture;
		}
		return segments[0].texture;
	}
}
