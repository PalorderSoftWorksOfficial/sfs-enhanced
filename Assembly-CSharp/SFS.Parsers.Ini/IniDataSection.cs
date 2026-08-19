using System;
using System.Collections.Generic;

namespace SFS.Parsers.Ini;

public class IniDataSection
{
	public readonly string name;

	public string comment;

	public int whitespacesBefore;

	public readonly Dictionary<string, IniDataEnv.Value> data = new Dictionary<string, IniDataEnv.Value>();

	public IniDataEnv.Value this[string index]
	{
		get
		{
			if (!data.ContainsKey(index))
			{
				return null;
			}
			return data[index];
		}
		set
		{
			data[index] = value;
		}
	}

	public IniDataSection(string name)
	{
		if (name.Contains("\n") || name.Contains("\r\n"))
		{
			throw new Exception("Section name is invalid!");
		}
		this.name = name;
	}

	public Dictionary<string, string> Simplify()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, IniDataEnv.Value> datum in data)
		{
			dictionary[datum.Key] = datum.Value;
		}
		return dictionary;
	}
}
