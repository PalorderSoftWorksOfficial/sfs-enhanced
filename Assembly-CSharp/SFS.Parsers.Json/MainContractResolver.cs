using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SFS.Parsers.Json;

public class MainContractResolver : DefaultContractResolver
{
	protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
	{
		IList<JsonProperty> source = base.CreateProperties(type, memberSerialization);
		List<JsonProperty> list = new List<JsonProperty>();
		JsonProperty[] array = source.ToArray();
		foreach (JsonProperty jsonProperty in array)
		{
			if (jsonProperty.Writable && !ExclusionList.Exclude(jsonProperty))
			{
				list.Add(jsonProperty);
			}
		}
		return list;
	}
}
