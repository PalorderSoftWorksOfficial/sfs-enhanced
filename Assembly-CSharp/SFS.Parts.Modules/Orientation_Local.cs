using System;
using SFS.Variables;

namespace SFS.Parts.Modules;

[Serializable]
public class Orientation_Local : Obs<Orientation>
{
	protected override bool IsEqual(Orientation a, Orientation b)
	{
		if (a != b)
		{
			if (a != null && b != null && a.x == b.x && a.y == b.y)
			{
				return a.z == b.z;
			}
			return false;
		}
		return true;
	}
}
