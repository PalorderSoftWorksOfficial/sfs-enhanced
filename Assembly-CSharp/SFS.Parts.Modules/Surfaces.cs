using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

public class Surfaces
{
	public readonly Vector2[] points;

	public readonly Transform owner;

	public readonly bool loop;

	public Surfaces(Vector2[] points, bool loop, Transform owner)
	{
		this.owner = owner;
		this.points = points;
		this.loop = loop;
	}

	public Line2[] GetSurfacesWorld()
	{
		if (points.Length < 2)
		{
			return new Line2[0];
		}
		Vector2[] array = ((IEnumerable<Vector2>)points).Select((Func<Vector2, Vector2>)((Vector2 p) => owner.TransformPoint(p))).ToArray();
		Line2[] array2 = new Line2[loop ? array.Length : (array.Length - 1)];
		for (int num = 0; num < array.Length - 1; num++)
		{
			array2[num] = new Line2(array[num], array[num + 1]);
		}
		if (loop)
		{
			array2[array2.Length - 1] = new Line2(array[array.Length - 1], array[0]);
		}
		return array2;
	}
}
