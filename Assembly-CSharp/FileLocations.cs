using System;
using System.Globalization;
using SFS.IO;
using UnityEngine;
using UnityEngine.Analytics;

public static class FileLocations
{
	public static FolderPath SolarSystemsFolder => BaseFolder.Extend("Custom Solar Systems").CreateFolder();

	public static FolderPath PublicTranslationFolder => BaseFolder.Extend(Application.isEditor ? "Translations" : "Custom Translations").CreateFolder();

	public static FolderPath TranslationCacheFolder => CacheFolder.Extend("Translations").CreateFolder();

	public static FolderPath BlueprintsFolder => SavingFolder.Extend("Blueprints");

	public static FolderPath WorldsFolder => SavingFolder.Extend("Worlds");

	private static FolderPath SavingFolder => BaseFolder.Extend((Application.isMobilePlatform || Application.isEditor) ? "Saving" : "/../Saving");

	private static FolderPath CacheFolder => BaseFolder.Extend("Cache").CreateFolder();

	public static FolderPath LogsFolder => BaseFolder.Extend("Logs").CreateFolder();

	public static FolderPath BaseFolder => new FolderPath((SaveInGameFolder ? Application.dataPath : Application.persistentDataPath) + (Application.isEditor ? "/Editor" : ""));

	private static bool SaveInGameFolder
	{
		get
		{
			if (!Application.isEditor && SystemInfo.operatingSystemFamily != OperatingSystemFamily.Windows)
			{
				return SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
			}
			return true;
		}
	}

	public static FilePath GetSettingsPath(string name)
	{
		return SavingFolder.Extend("Settings").CreateFolder().ExtendToFile(name + ".txt");
	}

	public static void IncreaseCount(string name)
	{
		GetNotificationsPath(name).WriteText((GetCount(name) + 1).ToString());
	}

	public static int GetCount(string name)
	{
		FilePath notificationsPath = GetNotificationsPath(name);
		if (!notificationsPath.FileExists() || !int.TryParse(notificationsPath.ReadText(), out var result))
		{
			return 0;
		}
		return result;
	}

	public static bool GetOneTimeNotification(string name)
	{
		FilePath notificationsPath = GetNotificationsPath(name);
		if (notificationsPath.FileExists())
		{
			return false;
		}
		Write(notificationsPath);
		return true;
	}

	public static bool GetOneTimeNotification_Repeated(string name, TimeSpan repeatTime, int repeatSessions, bool onParseFailed)
	{
		FilePath notificationsPath = GetNotificationsPath(name);
		if (!notificationsPath.FileExists())
		{
			Write(notificationsPath);
			return true;
		}
		string[] array = notificationsPath.ReadText().Split('|', StringSplitOptions.None);
		DateTime result = default(DateTime);
		int result2 = 0;
		if (array.Length != 2 || !DateTime.TryParse(array[0], out result) || !int.TryParse(array[1], out result2))
		{
			Write(notificationsPath);
			return onParseFailed;
		}
		if (DateTime.Now > result + repeatTime || AnalyticsSessionInfo.sessionCount > result2 + repeatSessions)
		{
			Write(notificationsPath);
			return true;
		}
		return false;
	}

	public static bool HasNotification(string name)
	{
		return GetNotificationsPath(name).FileExists();
	}

	public static void WriteNotification(string name)
	{
		FilePath notificationsPath = GetNotificationsPath(name);
		if (!notificationsPath.FileExists())
		{
			Write(notificationsPath);
		}
	}

	private static void Write(FilePath path)
	{
		path.WriteText(DateTime.Now.ToString(CultureInfo.InvariantCulture) + "|" + AnalyticsSessionInfo.sessionCount);
	}

	public static FilePath GetNotificationsPath(string name)
	{
		return SavingFolder.ExtendToFile("Notifications/" + name + ".txt", createNecessaryFolders: true);
	}
}
