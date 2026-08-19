using System;
using SFS.Variables;

namespace SFS.World;

[Serializable]
public class Staging_Local : Obs_Destroyable<Staging>
{
	protected override bool IsEqual(Staging a, Staging b)
	{
		return a == b;
	}
}
