using System;
using System.Text;
using Steamworks;
using UnityEngine;

[Serializable]
public class PlatformUtilities
{
	public string SocialToken;

	public bool initialized;

	public bool useSocial;

	public void Initialize()
	{
		if (!useSocial)
		{
			try
			{
				Hash128 hash = default(Hash128);
				string text = SystemInfo.deviceUniqueIdentifier;
				if (text == "n/a")
				{
					string text2 = PlayerPrefs.GetString("local_sharing_id", "None");
					if (text2 == "None")
					{
						text = MakeID();
						PlayerPrefs.SetString("local_sharing_id", text);
					}
					else
					{
						text = text2;
					}
				}
				HashUtilities.ComputeHash128(Encoding.UTF8.GetBytes(text), ref hash);
				initialized = true;
				SocialToken = hash.ToString();
				return;
			}
			catch (Exception message)
			{
				initialized = false;
				Debug.Log(message);
				throw;
			}
		}
		GetPlatformID(delegate(bool success, string id)
		{
			if (success && !string.IsNullOrEmpty(id))
			{
				initialized = true;
				SocialToken = id;
			}
		});
	}

	public string MakeID()
	{
		string text = "abcdefghijklmnopqrstuvwxyz0123456789";
		string text2 = null;
		for (int i = 0; i < 16; i++)
		{
			text2 += text[UnityEngine.Random.Range(0, text.Length)];
		}
		return text2 + (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
	}

	public void GetPlatformID(Action<bool, string> callback)
	{
		if (!SteamManager.Initialized)
		{
			callback(arg1: false, null);
		}
		callback(arg1: true, SteamUser.GetSteamID().ToString());
	}
}
