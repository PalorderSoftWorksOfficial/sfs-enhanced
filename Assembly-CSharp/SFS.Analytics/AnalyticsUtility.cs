using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.Networking;

namespace SFS.Analytics;

public static class AnalyticsUtility
{
	public static void SendEvent(string eventName, params (string, object)[] eventData)
	{
		SendEvent(eventName, eventData.ToDictionary(((string, object) x) => x.Item1, ((string, object) x) => x.Item2));
	}

	public static void SendEvent(string eventName, Dictionary<string, object> eventData)
	{
		if (Application.isEditor)
		{
			return;
		}
		List<Parameter> list = new List<Parameter>();
		foreach (string key in eventData.Keys)
		{
			object obj = eventData[key];
			Parameter parameter;
			if (!(obj is string parameterValue))
			{
				if (!(obj is float num))
				{
					if (!(obj is int num2))
					{
						if (!(obj is bool flag))
						{
							throw new ArgumentException($"Invalid type for value of key '{key}' ({obj.GetType()}).");
						}
						parameter = new Parameter(key, flag.ToString());
					}
					else
					{
						parameter = new Parameter(key, num2);
					}
				}
				else
				{
					parameter = new Parameter(key, num);
				}
			}
			else
			{
				parameter = new Parameter(key, parameterValue);
			}
			Parameter item = parameter;
			list.Add(item);
		}
		FirebaseAnalytics.LogEvent(eventName, list.ToArray());
	}

	public static void SendEvent(string eventName)
	{
		FirebaseAnalytics.LogEvent(eventName);
	}

	public static void SetUserProperty(string name, string property)
	{
		FirebaseAnalytics.SetUserProperty(name, property);
	}

	public static void SendEventToJordiServer(string eventName, params (string, object)[] eventData)
	{
		if (!Application.isEditor)
		{
			try
			{
				Dictionary<string, string> dictionary = eventData.ToDictionary(((string, object) a) => a.Item1, ((string, object) b) => b.Item2.ToString());
				dictionary.Add("%event_name%", eventName);
				UnityWebRequest uwr = UnityWebRequest.Post("https://jmnet.one/san/post.php", dictionary);
				uwr.SendWebRequest().completed += delegate
				{
					uwr.Dispose();
				};
			}
			catch
			{
			}
		}
		if (!Application.isEditor)
		{
			SendEvent(eventName, eventData);
		}
	}
}
