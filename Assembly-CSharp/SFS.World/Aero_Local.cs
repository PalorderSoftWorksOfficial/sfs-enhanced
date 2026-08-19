using System;
using SFS.Variables;
using SFS.World.Drag;

namespace SFS.World;

[Serializable]
public class Aero_Local : Obs<AeroModule>
{
	protected override bool IsEqual(AeroModule a, AeroModule b)
	{
		return a == b;
	}
}
