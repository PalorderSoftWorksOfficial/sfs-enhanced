using System;
using SFS.Variables;

namespace SFS.World;

[Serializable]
public class Player_Local : Obs_Destroyable<Player>
{
	protected override bool IsEqual(Player a, Player b)
	{
		return a == b;
	}
}
