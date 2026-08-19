using System;

namespace SFS.Variables;

[Serializable]
public class Double_Local : Obs<double>
{
	protected override bool IsEqual(double a, double b)
	{
		return a == b;
	}
}
