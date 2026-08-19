using System;
using System.Linq;
using SFS.Variables;

namespace SFS.Parts.Modules;

public class Own_Size : OwnModule
{
	[Serializable]
	public class AllowedSize
	{
		public Float_Reference size;

		public float maxAllowedSize;
	}

	public AllowedSize[] allowed;

	public override bool IsPremium => !allowed.All((AllowedSize a) => a.size.Value < a.maxAllowedSize + 0.01f);
}
