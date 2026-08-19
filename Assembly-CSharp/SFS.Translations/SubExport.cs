using System;

namespace SFS.Translations;

public class SubExport
{
	public const string Placeholder = "placeholder";

	public const string Lowercase = "lowercase";

	public const string Multiplication_S = "multi_s";

	public Func<string, string> translationModifier;

	private SubExport()
	{
	}

	public static SubExport CreateExport(string name)
	{
		return name switch
		{
			"lowercase" => new SubExport
			{
				translationModifier = (string sub) => sub.ToLower()
			}, 
			"placeholder" => new SubExport
			{
				translationModifier = (string sub) => "Empty sub"
			}, 
			"multi_s" => new SubExport
			{
				translationModifier = (string sub) => sub + "s"
			}, 
			_ => new SubExport
			{
				translationModifier = (string _) => "unknown export"
			}, 
		};
	}
}
