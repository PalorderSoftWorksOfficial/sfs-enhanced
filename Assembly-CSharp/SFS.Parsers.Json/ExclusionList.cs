using System.Linq;
using Newtonsoft.Json.Serialization;
using SFS.Parsers.Json.Exclusions;
using UnityEngine;

namespace SFS.Parsers.Json;

public class ExclusionList
{
	public static readonly BaseExclusion[] exclusions = new BaseExclusion[3]
	{
		new Inclusion<Vector2>("x", "y"),
		new Inclusion<Color>("r", "g", "b", "a"),
		new Inclusion<Double3>("x", "y", "z")
	};

	public static bool Exclude(JsonProperty prop)
	{
		return exclusions.Where((BaseExclusion ex) => ex.Exclude(prop)).Count() > 0;
	}
}
