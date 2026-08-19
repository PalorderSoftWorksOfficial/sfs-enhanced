using System;

namespace SFS.Variables;

[Serializable]
public class Double3_Local : Obs<Double3>
{
	protected override bool IsEqual(Double3 a, Double3 b)
	{
		return a == b;
	}
}
