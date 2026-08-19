using System;
using SFS.Variables;

namespace SFS.World;

[Serializable]
public class Local_Resources : Obs_Destroyable<Resources>
{
	protected override bool IsEqual(Resources a, Resources b)
	{
		return a == b;
	}
}
