using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using SFS.Core;
using SFS.IO;
using SFS.UI;
using UnityEngine;

namespace SFS.Translations;

public class LanguageLoader : MonoBehaviour
{
	[Serializable]
	public class LanguageReference
	{
		public string name;

		public bool custom;

		[NonSerialized]
		public string displayName;

		public LanguageReference()
		{
		}

		public LanguageReference(string name, string displayName, bool custom)
		{
			this.name = name;
			this.custom = custom;
			this.displayName = displayName;
		}
	}

	private class CustomWebClient : WebClient
	{
		public int timeoutMs;

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest webRequest = base.GetWebRequest(address);
			webRequest.Timeout = timeoutMs;
			return webRequest;
		}
	}

	public List<Font> fonts = new List<Font>();

	private const string LinksFileUrl = "https://raw.githubusercontent.com/Stef-Moroyna/Spaceflight-Simulator-Translations/master/1.5.3/links.txt";

	private int onlineTranslationVersion = -1;

	private Dictionary<string, (string link, string localizedName)> translationLinks = new Dictionary<string, (string, string)>();

	private Action onInitialized;

	public bool IsInitialized { get; private set; }

	private void Awake()
	{
		string version = Application.version;
		FilePath examplePath = FileLocations.PublicTranslationFolder.ExtendToFile("Example.txt");
		FilePath exampleVersionPath = FileLocations.GetNotificationsPath("Translation_Example_Version");
		new Thread((ParameterizedThreadStart)delegate
		{
			if (!exampleVersionPath.FileExists() || !(exampleVersionPath.ReadText() == version) || !examplePath.FileExists())
			{
				examplePath.WriteText(TranslationSerialization.Serialize(TranslationSerialization.CreateTranslation<SFS_Translation>()));
				exampleVersionPath.WriteText(version);
			}
		}).Start();
		Loc.OnChange += new Action(Canvas.ForceUpdateCanvases);
		StartCoroutine(WaitForInitialization());
		DownloadLinks();
	}

	private void DownloadLinks()
	{
		FilePath linksCacheFile = FileLocations.TranslationCacheFolder.ExtendToFile("links.txt");
		if (Application.internetReachability != NetworkReachability.NotReachable)
		{
			bool isReady = false;
			if (!linksCacheFile.FileExists())
			{
				Menu.loading.Open("Loading languages...", () => isReady);
			}
			CustomWebClient customWebClient = new CustomWebClient();
			customWebClient.timeoutMs = 2500;
			customWebClient.DownloadStringAsync(new Uri("https://raw.githubusercontent.com/Stef-Moroyna/Spaceflight-Simulator-Translations/master/1.5.3/links.txt"));
			customWebClient.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs eventData)
			{
				ActionQueue.main.QueueAction(delegate
				{
					if (eventData.Error == null)
					{
						LoadLinks(eventData.Result);
						linksCacheFile.WriteText(eventData.Result);
					}
					IsInitialized = true;
					isReady = true;
					Menu.loading.Close();
				});
			};
		}
		else
		{
			if (linksCacheFile.FileExists())
			{
				LoadLinks(linksCacheFile.ReadText());
			}
			IsInitialized = true;
		}
	}

	private void LoadLinks(string linksFileContent)
	{
		string[] array = linksFileContent.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
		foreach (string text in array)
		{
			if (text.StartsWith("ver:"))
			{
				onlineTranslationVersion = int.Parse(text.Substring(4));
				continue;
			}
			string text2 = text.Split(';', StringSplitOptions.None)[1];
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
			string item = text.Split(';', StringSplitOptions.None)[0];
			translationLinks[fileNameWithoutExtension] = (text2, item);
		}
	}

	public void RunAfterInitialization(Action a)
	{
		if (IsInitialized)
		{
			a();
		}
		else
		{
			onInitialized = (Action)Delegate.Combine(onInitialized, a);
		}
	}

	public string GetDisplayName(LanguageReference a)
	{
		if (a.custom)
		{
			return a.displayName;
		}
		foreach (LanguageReference item in from language in GetAvailableLanguages()
			where language != null
			select language)
		{
			if (a.name == item.name)
			{
				return item.displayName;
			}
		}
		if (a.name == null)
		{
			return "English";
		}
		return "-";
	}

	public LanguageReference[] GetAvailableLanguages()
	{
		List<LanguageReference> list = new List<LanguageReference>();
		list.Add(new LanguageReference("English", "English", custom: false));
		list.AddRange(translationLinks.Select((KeyValuePair<string, (string link, string localizedName)> pair) => new LanguageReference(pair.Key, pair.Value.localizedName, custom: false)));
		if (!Application.isEditor)
		{
			FilePath[] array = FileLocations.PublicTranslationFolder.GetFilesInFolder(recursively: false).ToArray();
			if (array.Length != 0)
			{
				list.Add(null);
			}
			FilePath[] array2 = array;
			foreach (FilePath filePath in array2)
			{
				if (filePath.Extension == "txt" && filePath.FileName != "Example.txt" && filePath.FileName != "READ_ME.txt")
				{
					list.Add(new LanguageReference(filePath.CleanFileName, filePath.CleanFileName + " (Custom)", custom: true));
				}
			}
		}
		return list.ToArray();
	}

	public void LoadLanguage(LanguageReference language, I_MsgLogger logger, Action<bool> callback)
	{
		try
		{
			if (language.name == null || language.name == "English")
			{
				LoadDefaultEnglish();
				return;
			}
			if (language.custom)
			{
				FilePath filePath = FileLocations.PublicTranslationFolder.ExtendToFile(language.name + ".txt");
				if (filePath.FileExists())
				{
					Loc.SetLanguage("Non-Default", TranslationSerialization.Deserialize(filePath.ReadText(), out var unused, out var missing, out var _));
					ShowDeserializationReport(missing, unused, delegate
					{
						callback(obj: true);
					});
				}
				else
				{
					LoadDefaultEnglish();
				}
				return;
			}
			DownloadLanguage(language.name, delegate(bool success, string content)
			{
				if (!success)
				{
					LoadDefaultEnglish();
					Menu.read.Open(() => "Loading language failed: No internet", delegate
					{
						callback(obj: false);
					});
				}
				else if (content == null)
				{
					LoadDefaultEnglish();
				}
				else
				{
					Loc.SetLanguage("Non-Default", TranslationSerialization.Deserialize(content, out var unused2, out var missing2, out var _));
					if (Application.isEditor)
					{
						ShowDeserializationReport(missing2, unused2, delegate
						{
							callback(obj: true);
						});
					}
					else
					{
						callback(obj: true);
					}
				}
			});
		}
		catch (Exception exception)
		{
			logger.Log("Could not load language, most likely a format issue");
			Debug.LogException(exception);
			LoadDefaultEnglish();
		}
		void LoadDefaultEnglish()
		{
			Loc.SetLanguage("Default", TranslationSerialization.CreateTranslation<SFS_Translation>());
			callback(obj: true);
		}
	}

	private void DownloadLanguage(string name, Action<bool, string> onLoaded)
	{
		FilePath cacheFile = FileLocations.TranslationCacheFolder.ExtendToFile(name + ".txt");
		if (cacheFile.FileExists())
		{
			string[] array = cacheFile.ReadText().Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
			int num = int.Parse(array[0]);
			if (onlineTranslationVersion != -1 && onlineTranslationVersion != num)
			{
				UpdateCache();
				return;
			}
			string[] array2 = new string[array.Length - 1];
			for (int i = 1; i < array.Length; i++)
			{
				array2[i - 1] = array[i];
			}
			string arg = string.Join("\n", array2);
			onLoaded(arg1: true, arg);
		}
		else
		{
			UpdateCache();
		}
		void UpdateCache()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				onLoaded(arg1: false, null);
			}
			else if (!translationLinks.ContainsKey(name))
			{
				onLoaded(arg1: true, null);
			}
			else
			{
				CustomWebClient customWebClient = new CustomWebClient();
				customWebClient.timeoutMs = 5000;
				customWebClient.DownloadStringAsync(new Uri(translationLinks[name].link));
				customWebClient.DownloadStringCompleted += delegate(object _, DownloadStringCompletedEventArgs eventData)
				{
					if (eventData.Error != null)
					{
						onLoaded(arg1: false, null);
					}
					else
					{
						cacheFile.WriteText(onlineTranslationVersion + "\n" + eventData.Result);
						onLoaded(arg1: true, eventData.Result);
					}
				};
			}
		}
	}

	private static void ShowDeserializationReport(List<FieldReference> missing, List<FieldReference> unused, Action callback)
	{
		if (unused.Count + missing.Count == 0)
		{
			callback();
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Your custom translation is out of date");
		stringBuilder.AppendLine("(English will be used for missing translations)");
		stringBuilder.AppendLine("");
		if (missing.Count > 0)
		{
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("Missing:");
			foreach (FieldReference item in missing)
			{
				stringBuilder.AppendLine(item.group + " / " + item.name);
			}
		}
		if (unused.Count > 0)
		{
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("Unnecessary:");
			foreach (FieldReference item2 in unused)
			{
				stringBuilder.AppendLine(item2.group + " / " + item2.name);
			}
		}
		Menu.read.ShowReport(stringBuilder, callback);
	}

	private IEnumerator WaitForInitialization()
	{
		while (!IsInitialized)
		{
			yield return new WaitForEndOfFrame();
		}
		onInitialized?.Invoke();
	}
}
