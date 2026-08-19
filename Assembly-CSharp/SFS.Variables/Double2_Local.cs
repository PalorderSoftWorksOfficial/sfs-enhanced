using System;

namespace SFS.Variables;

[Serializable]
public class Double2_Local : Obs<Double2>
{
	protected override bool IsEqual(Double2 a, Double2 b)
	{
		return a == b;
	}
}
