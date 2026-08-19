using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Analytics;

public class ABTests : MonoBehaviour
{
	public Dictionary<string, string> tests = new Dictionary<string, string>();

	public string abTestEventName = "ab_test_event";

	public bool testsLoaded;

	public Action<bool> testsLoadedCallback;

	private void Awake()
	{
		FbRemoteSettings main = FbRemoteSettings.main;
		main.valuesLoadedCallback = (Action)Delegate.Combine(main.valuesLoadedCallback, new Action(LoadTests));
	}

	public void CompileTests()
	{
		tests.Add(abTestEventName, "");
	}

	private void LoadTests()
	{
		if (!FbRemoteSettings.main.defaultsLoaded && !FbRemoteSettings.main.valuesLoaded)
		{
			Debug.LogError("Cannot load AB tests, no remote or default values loaded!");
			testsLoadedCallback(obj: false);
			return;
		}
		CompileTests();
		foreach (string key in tests.Keys)
		{
			string text = FbRemoteSettings.GetString(key);
			if (text == null)
			{
				Debug.LogError("Remote config value for key \"" + key + "\" is null!");
			}
			tests[key] = text;
			if (FbRemoteSettings.main.debugMessages)
			{
				Debug.Log("Test loaded - " + key + ": " + tests[key]);
			}
		}
		testsLoaded = true;
		testsLoadedCallback(obj: true);
	}
}
