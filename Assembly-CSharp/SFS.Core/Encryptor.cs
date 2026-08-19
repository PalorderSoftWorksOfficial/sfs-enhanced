using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SFS.Core;

public static class Encryptor
{
	private static string testCase = "^a^";

	private static string EncryptionKey => C("^I will be obfuscated, hopefuly that's good enough^");

	private static byte[] EncryptionIV => Convert.FromBase64String(C("^B2E6Ht05+NrcclbMe78/Fg==^"));

	public static string Encrypt(string input, SecurityMode securityMode, string keyOffset = "", string forcedDeviceId = null)
	{
		if (securityMode == SecurityMode.GlobalKey)
		{
			return Encrypt(input, Encoding.UTF8.GetBytes(EncryptionKey + keyOffset));
		}
		return Encrypt(input, SecurityMode.GlobalKey, (forcedDeviceId ?? SystemInfo.deviceUniqueIdentifier) + C("^Some salt^") + keyOffset);
	}

	public static bool Decrypt(string input, out string output, SecurityMode securityMode, string keyOffset = "", string forcedDeviceId = null)
	{
		if (securityMode == SecurityMode.GlobalKey)
		{
			return Decrypt(input, Encoding.UTF8.GetBytes(EncryptionKey + keyOffset), out output);
		}
		return Decrypt(input, out output, SecurityMode.GlobalKey, (forcedDeviceId ?? SystemInfo.deviceUniqueIdentifier) + C("^Some salt^") + keyOffset);
	}

	private static string Encrypt(string input, byte[] key)
	{
		using SHA256Managed sHA256Managed = new SHA256Managed();
		using AesManaged aesManaged = new AesManaged();
		using MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
		using MemoryStream memoryStream2 = new MemoryStream();
		using CryptoStream cryptoStream = new CryptoStream(memoryStream2, aesManaged.CreateEncryptor(sHA256Managed.ComputeHash(key), EncryptionIV), CryptoStreamMode.Write);
		memoryStream.CopyTo(cryptoStream);
		cryptoStream.Flush();
		cryptoStream.Close();
		return Convert.ToBase64String(memoryStream2.ToArray());
	}

	private static bool Decrypt(string input, byte[] key, out string value)
	{
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using (SHA256Managed sHA256Managed = new SHA256Managed())
			{
				using AesManaged aesManaged = new AesManaged();
				using MemoryStream stream = new MemoryStream(Convert.FromBase64String(input));
				using CryptoStream cryptoStream = new CryptoStream(stream, aesManaged.CreateDecryptor(sHA256Managed.ComputeHash(key), EncryptionIV), CryptoStreamMode.Read);
				cryptoStream.CopyTo(memoryStream);
			}
			value = Encoding.UTF8.GetString(memoryStream.ToArray());
			return true;
		}
		catch
		{
		}
		value = null;
		return false;
	}

	public static string C(string s)
	{
		if (testCase != "a")
		{
			return s.Substring(1, s.Length - 1 - 1);
		}
		return s;
	}
}
