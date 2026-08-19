using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StraightMesh : BasicMeshData
{
	public Vector2 size;

	public List<Vector2> GetCurveData()
	{
		return new List<Vector2>
		{
			Vector2.zero,
			size
		};
	}
}
