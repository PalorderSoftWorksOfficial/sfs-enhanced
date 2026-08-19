using System.Collections.Generic;

namespace SFS.Parsers.Ini;

public class IniDataEnv
{
	public class Value
	{
		public string value;

		public string preLineComment;

		public string aftLineComment;

		public int whitespacesBefore;

		public Value(string value)
		{
			this.value = value;
		}

		public static implicit operator string(Value value)
		{
			return value.value;
		}

		public static implicit operator Value(string value)
		{
			return new Value(value);
		}
	}

	public readonly Dictionary<string, IniDataSection> sections = new Dictionary<string, IniDataSection>();

	public readonly IniDataSection Global = new IniDataSection("Globals");

	public Value this[string name]
	{
		get
		{
			return Global[name];
		}
		set
		{
			Global[name] = value;
		}
	}

	public Value this[string name, string section]
	{
		get
		{
			return GetSection(section)[name];
		}
		set
		{
			GetSection(section)[name] = value;
		}
	}

	public IniDataSection GetSection(string name)
	{
		if (sections.ContainsKey(name))
		{
			return sections[name];
		}
		IniDataSection iniDataSection = new IniDataSection(name);
		sections[name] = iniDataSection;
		return iniDataSection;
	}
}
