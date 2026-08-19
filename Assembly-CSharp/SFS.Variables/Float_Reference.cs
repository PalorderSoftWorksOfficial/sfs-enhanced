using System;

namespace SFS.Variables;

[Serializable]
public class Float_Reference : Double_Reference
{
	public new float Value
	{
		get
		{
			return (float)base.Value;
		}
		set
		{
			base.Value = value;
		}
	}

	protected override bool IsEqual(double a, double b)
	{
		return a == b;
	}
}
