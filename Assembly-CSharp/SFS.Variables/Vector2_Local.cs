using System;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public class Vector2_Local : Obs<Vector2>
{
	protected override bool IsEqual(Vector2 a, Vector2 b)
	{
		return a == b;
	}
}
