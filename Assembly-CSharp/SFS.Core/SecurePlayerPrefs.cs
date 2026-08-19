using UnityEngine;

namespace SFS.Core;

public static class SecurePlayerPrefs
{
	public static void SetString(string key, string value, SecurityMode securityMode)
	{
		PlayerPrefs.SetString(key, Encryptor.Encrypt(value, securityMode));
	}

	public static bool GetString(string key, out string value, SecurityMode securityMode)
	{
		return Encryptor.Decrypt(PlayerPrefs.GetString(key, ""), out value, securityMode);
	}
}
