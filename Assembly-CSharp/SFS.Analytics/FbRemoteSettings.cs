using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

namespace SFS.Analytics;

public class FbRemoteSettings : MonoBehaviour
{
	public static FbRemoteSettings main;

	private FirebaseRemoteConfig settingsInstance;

	private readonly TimeSpan cacheTimeout = TimeSpan.FromHours(2.0);

	public bool defaultsLoaded;

	public bool valuesLoaded;

	public bool useFirebase = true;

	private bool firebaseReady;

	private bool setupRan;

	public Action valuesLoadedCallback;

	public bool debugMessages;

	private Dictionary<string, object> defaults = new Dictionary<string, object>
	{
		{ "ab_test_event", "test" },
		{ "remote_config_fetch_test", "local value" },
		{ "Custom_Parts", false },
		{ "Custom_Skins", false },
		{ "Show_Development_Menu", false },
		{ "allow_local_restore_iOS", true },
		{ "Pick_Language_AB", true },
		{ "sharingDomain", "https://sharing.spaceflightsimulator.app" },
		{ "sharingUserAgent", "s&1FS&xdkf2r5k9p2zU!PDYXW$bqae" },
		{ "Tut_Build_Popups", true },
		{ "Example_Rockets", true },
		{ "Video_Tutorials", true },
		{ "update_popup_v3", "" },
		{ "Version_Display_Data", "" },
		{ "Hard_Game_Msg", true }
	};

	private void Awake()
	{
		main = this;
		if (debugMessages)
		{
			Debug.Log("Setting up firebase");
			valuesLoadedCallback = (Action)Delegate.Combine(valuesLoadedCallback, (Action)delegate
			{
				string text = GetString("remote_config_fetch_test");
				Debug.Log("Firebase config loading test\n\n" + text);
			});
		}
		FirebaseUtility firebaseUtility = FirebaseUtility.main;
		firebaseUtility.initCallback = (Action<bool>)Delegate.Combine(firebaseUtility.initCallback, (Action<bool>)delegate(bool success)
		{
			firebaseReady = success;
			if (success)
			{
				settingsInstance = FirebaseRemoteConfig.DefaultInstance;
				firebaseReady = true;
				if (debugMessages)
				{
					Debug.Log("Setting defaults/loading values.");
				}
				FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled: true);
				SetDefaults();
				LoadValues();
			}
			else
			{
				Debug.LogError("Error fetch Firebase dependencies!");
			}
			if (debugMessages)
			{
				Debug.Log($"Dependency was successful: {success}");
			}
		});
	}

	private void SetDefaults()
	{
		settingsInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(delegate(Task task)
		{
			if (debugMessages)
			{
				Debug.Log($"Defaults loading was successful: {task.IsCompletedSuccessfully}");
			}
			defaultsLoaded = task.IsCompletedSuccessfully;
		});
	}

	private void LoadValues()
	{
		settingsInstance.FetchAsync(cacheTimeout).ContinueWithOnMainThread(delegate(Task task)
		{
			if (debugMessages)
			{
				Debug.Log($"Values fetch success: {task.IsCompletedSuccessfully & (settingsInstance.Info.LastFetchStatus == LastFetchStatus.Success)}");
			}
			if (!task.IsCompleted)
			{
				Debug.LogError("Firebase remote settings fetch task could not be completed!");
			}
			else if (settingsInstance.Info.LastFetchStatus != LastFetchStatus.Success)
			{
				Debug.LogError("Firebase remote settings fetch task was not completed successfully!");
			}
			else
			{
				settingsInstance.ActivateAsync().ContinueWithOnMainThread((Action<Task<bool>>)delegate
				{
					valuesLoaded = true;
					valuesLoadedCallback?.Invoke();
				});
			}
		});
	}

	private static ConfigValue FetchValue(string valueName)
	{
		return main.settingsInstance.GetValue(valueName);
	}

	public static string GetString(string keyName)
	{
		return FetchValue(keyName).StringValue;
	}

	public static string GetString(string keyName, string defaultValue)
	{
		if (!main.valuesLoaded)
		{
			return defaultValue;
		}
		return GetString(keyName);
	}

	public static bool GetBool(string keyName)
	{
		return FetchValue(keyName).BooleanValue;
	}

	public static bool GetBool(string keyName, bool defaultValue)
	{
		if (!main.valuesLoaded)
		{
			return defaultValue;
		}
		return GetBool(keyName);
	}

	public static long GetLong(string keyName)
	{
		return FetchValue(keyName).LongValue;
	}

	public static long GetLong(string keyName, long defaultValue)
	{
		if (!main.valuesLoaded)
		{
			return defaultValue;
		}
		return GetLong(keyName);
	}

	public static double GetDouble(string keyName)
	{
		return FetchValue(keyName).DoubleValue;
	}

	public static double GetDouble(string keyName, double defaultValue)
	{
		if (!main.valuesLoaded)
		{
			return defaultValue;
		}
		return GetDouble(keyName);
	}

	public static int GetInt(string keyName)
	{
		return (int)FetchValue(keyName).DoubleValue;
	}

	public static int GetInt(string keyName, int defaultValue)
	{
		if (!main.valuesLoaded)
		{
			return defaultValue;
		}
		return GetInt(keyName);
	}
}
