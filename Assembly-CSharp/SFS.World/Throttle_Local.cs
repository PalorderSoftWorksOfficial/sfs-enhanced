using System;
using SFS.Variables;

namespace SFS.World;

[Serializable]
public class Throttle_Local : Obs_Destroyable<Throttle>
{
	protected override bool IsEqual(Throttle a, Throttle b)
	{
		return a == b;
	}
}
