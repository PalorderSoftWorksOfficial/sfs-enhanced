using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Beebyte.Obfuscator;
using Newtonsoft.Json;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Parts;

[Serializable]
[Skip]
public class PartSave
{
	[JsonProperty("n")]
	public string name;

	[JsonProperty("p")]
	public Vector2 position;

	[JsonProperty("o")]
	public Orientation orientation;

	[JsonProperty("t")]
	public float temperature = float.NegativeInfinity;

	[JsonProperty("N")]
	public Dictionary<string, double> NUMBER_VARIABLES = new Dictionary<string, double>();

	[JsonProperty("B")]
	public Dictionary<string, bool> TOGGLE_VARIABLES = new Dictionary<string, bool>();

	[JsonProperty("T")]
	public Dictionary<string, string> TEXT_VARIABLES = new Dictionary<string, string>();

	public BurnMark.BurnSave burns;

	public static PartSave[] CreateSaves(Part[] parts)
	{
		PartSave[] array = new PartSave[parts.Length];
		for (int i = 0; i < parts.Length; i++)
		{
			array[i] = new PartSave(parts[i]);
		}
		return array;
	}

	public PartSave()
	{
	}

	public PartSave(Part part)
	{
		name = part.name;
		position = part.transform.localPosition;
		orientation = part.orientation.orientation.Value;
		temperature = part.temperature;
		if (part.burnMark != null)
		{
			burns = new BurnMark.BurnSave(part.burnMark.burn);
		}
		NUMBER_VARIABLES = part.variablesModule.doubleVariables.GetSaveDictionary();
		TOGGLE_VARIABLES = part.variablesModule.boolVariables.GetSaveDictionary();
		TEXT_VARIABLES = part.variablesModule.stringVariables.GetSaveDictionary();
	}

	public PartSave(string name, Vector2 position, Orientation orientation, Dictionary<string, double> NUMBER_VARIABLES, Dictionary<string, bool> TOGGLE_VARIABLES, Dictionary<string, string> TEXT_VARIABLES)
	{
		this.name = name;
		this.position = position;
		this.orientation = orientation;
		temperature = float.NegativeInfinity;
		this.NUMBER_VARIABLES = NUMBER_VARIABLES;
		this.TOGGLE_VARIABLES = TOGGLE_VARIABLES;
		this.TEXT_VARIABLES = TEXT_VARIABLES;
	}

	[OnSerializing]
	private void OnSerialization(StreamingContext context)
	{
		if (NUMBER_VARIABLES != null && NUMBER_VARIABLES.Count == 0)
		{
			NUMBER_VARIABLES = null;
		}
		if (TOGGLE_VARIABLES != null && TOGGLE_VARIABLES.Count == 0)
		{
			TOGGLE_VARIABLES = null;
		}
		if (TEXT_VARIABLES != null && TEXT_VARIABLES.Count == 0)
		{
			TEXT_VARIABLES = null;
		}
	}
}
