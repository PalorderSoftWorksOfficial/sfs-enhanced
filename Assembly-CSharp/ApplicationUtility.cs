using System;
using System.Collections.Generic;
using System.Diagnostics;
using SFS.Core;
using SFS.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class ApplicationUtility
{
	private static bool testingAccess = false;

	private static bool? internetAccess;

	private static List<Action<bool>> queue = new List<Action<bool>>();

	public static void Relaunch()
	{
		BasePath executablePath = new FolderPath(Application.dataPath).Extend("/../").ExtendToFile(Application.productName + ".exe");
		Application.quitting += delegate
		{
			Process.Start(executablePath);
		};
		Application.Quit();
	}

	public static void CheckInternetAccess(Action<bool> onResult, bool useCached = true)
	{
		if (internetAccess.HasValue & useCached)
		{
			onResult(internetAccess.Value);
			return;
		}
		lock (queue)
		{
			queue.Add(onResult);
		}
		RefreshInternetAccess();
	}

	public static void RefreshInternetAccess()
	{
		if (testingAccess)
		{
			return;
		}
		CleanLog("Testing Internet Access");
		testingAccess = true;
		try
		{
			UnityWebRequest uwr = UnityWebRequest.Get("https://raw.githubusercontent.com/Stef-Moroyna/Spaceflight-Simulator-Translations/master/do_not_delete.txt");
			uwr.SendWebRequest().completed += delegate
			{
				internetAccess = uwr.responseCode == 200;
				CleanLog("Internet reachability test returned: " + (uwr.responseCode == 200));
				uwr.Dispose();
				lock (queue)
				{
					foreach (Action<bool> item in queue)
					{
						item(internetAccess.Value);
					}
					queue.Clear();
					testingAccess = false;
				}
			};
		}
		catch
		{
			CleanLog("Failed to test internet access");
			testingAccess = false;
			lock (queue)
			{
				foreach (Action<bool> item2 in queue)
				{
					item2(internetAccess.Value);
				}
				queue.Clear();
				testingAccess = false;
			}
		}
	}

	public static void CleanLog(string message)
	{
		ActionQueue.main.QueueAction(delegate
		{
			StackTraceLogType stackTraceLogType = Application.GetStackTraceLogType(LogType.Log);
			Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			UnityEngine.Debug.Log(message);
			Application.SetStackTraceLogType(LogType.Log, stackTraceLogType);
		});
	}
}
