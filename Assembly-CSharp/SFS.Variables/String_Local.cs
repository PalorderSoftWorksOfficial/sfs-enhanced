using System;

namespace SFS.Variables;

[Serializable]
public class String_Local : Obs<string>
{
	protected override bool IsEqual(string a, string b)
	{
		return a == b;
	}
}
