using SFS.Translations;

namespace SFS;

public static class TranslationUtility
{
	public static Field State_ToOnOff(this bool a)
	{
		if (!a)
		{
			return Loc.main.State_Off;
		}
		return Loc.main.State_On;
	}
}
