using System;
using SFS.Parts;
using UnityEngine;

namespace SFS.World;

[Serializable]
public class PartJoint
{
	public Part a;

	public Part b;

	public Vector2 anchor;

	public PartJoint(Part a, Part b, Vector2 anchor)
	{
		this.a = a;
		this.b = b;
		this.anchor = anchor;
	}

	public Part GetOtherPart(Part part)
	{
		if (!(part == a))
		{
			if (!(part == b))
			{
				throw new Exception("Provided object is not linked to joint!");
			}
			return a;
		}
		return b;
	}

	public Vector2 GetRelativeAnchor(Part part)
	{
		if (!(part == a))
		{
			if (!(part == b))
			{
				throw new Exception("Provided object is not linked to joint!");
			}
			return -anchor;
		}
		return anchor;
	}
}
