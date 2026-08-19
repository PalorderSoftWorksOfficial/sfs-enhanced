using System;
using System.Collections.Generic;

namespace SFS.Translations;

public class Group : Attribute
{
	public bool hasSubs;

	public List<Func<string, string>> subExports = new List<Func<string, string>>();

	public string Name { get; }

	public Group(string name)
	{
		Name = name;
	}

	public Group(string name, params string[] exportNames)
		: this(name)
	{
		foreach (string name2 in exportNames)
		{
			subExports.Add(SubExport.CreateExport(name2).translationModifier);
		}
	}
}
