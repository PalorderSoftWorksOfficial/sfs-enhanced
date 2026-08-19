using System;
using SFS.Variables;

namespace SFS.World;

[Serializable]
public class Arrowkeys_Local : Obs_Destroyable<Arrowkeys>
{
	protected override bool IsEqual(Arrowkeys a, Arrowkeys b)
	{
		return a == b;
	}
}
