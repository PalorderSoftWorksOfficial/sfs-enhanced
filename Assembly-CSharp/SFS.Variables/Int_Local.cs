using System;

namespace SFS.Variables;

[Serializable]
public class Int_Local : Obs<int>
{
	protected override bool IsEqual(int a, int b)
	{
		return a == b;
	}
}
