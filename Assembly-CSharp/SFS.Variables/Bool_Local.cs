using System;

namespace SFS.Variables;

[Serializable]
public class Bool_Local : Obs<bool>
{
	protected override bool IsEqual(bool a, bool b)
	{
		return a == b;
	}
}
