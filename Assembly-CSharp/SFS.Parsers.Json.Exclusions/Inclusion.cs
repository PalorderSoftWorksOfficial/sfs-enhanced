using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace SFS.Parsers.Json.Exclusions;

public class Inclusion<T> : BaseExclusion
{
	private List<string> names;

	public Inclusion(params string[] names)
	{
		this.names = names.ToList();
	}

	public override bool Exclude(JsonProperty prop)
	{
		if (prop.DeclaringType != typeof(T))
		{
			return false;
		}
		return names.Where((string name) => prop.PropertyName == name).ToList().Count == 0;
	}
}
