using System.Text.RegularExpressions;

namespace SFS.Parsers.Regex;

public class SimpleRegex
{
	public System.Text.RegularExpressions.Regex Regex { get; private set; }

	public Match Match { get; private set; }

	public SimpleRegex(string regex)
	{
		Regex = new System.Text.RegularExpressions.Regex(regex, RegexOptions.Multiline | RegexOptions.CultureInvariant);
	}

	public bool Input(string input)
	{
		Match = Regex.Match(input);
		if (Match != null)
		{
			return Match.Success;
		}
		return false;
	}

	public bool Input(string input, int start)
	{
		Match = Regex.Match(input, start);
		if (Match != null)
		{
			return Match.Success;
		}
		return false;
	}

	public Group GetGroup(string name)
	{
		foreach (Group group in Match.Groups)
		{
			if (group.Name == name)
			{
				return group;
			}
		}
		return null;
	}

	public bool Next()
	{
		if (!Match.Success)
		{
			return false;
		}
		Match = Match.NextMatch();
		if (Match != null)
		{
			return Match.Success;
		}
		return false;
	}

	public static implicit operator SimpleRegex(System.Text.RegularExpressions.Regex regex)
	{
		return new SimpleRegex("")
		{
			Regex = regex
		};
	}
}
