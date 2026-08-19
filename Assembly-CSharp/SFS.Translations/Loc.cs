using System.Collections.Generic;
using System.Reflection;
using SFS.Variables;

namespace SFS.Translations;

public static class Loc
{
	public static string languageName;

	public static SFS_Translation main;

	public static Dictionary<string, Field> fields;

	public static Event_Local OnChange;

	public static void SetLanguage(string newLanguageName, SFS_Translation language)
	{
		if (newLanguageName == languageName)
		{
			return;
		}
		languageName = newLanguageName;
		main = language;
		fields = new Dictionary<string, Field>();
		foreach (var fieldReference in TranslationSerialization.GetFieldReferences<SFS_Translation>())
		{
			PropertyInfo item = fieldReference.Item1;
			if (item.GetValue(main) is Field value)
			{
				fields[item.Name] = value;
			}
		}
		OnChange?.Invoke();
	}

	static Loc()
	{
		OnChange = new Event_Local();
		SetLanguage("Default", TranslationSerialization.CreateTranslation<SFS_Translation>());
	}
}
