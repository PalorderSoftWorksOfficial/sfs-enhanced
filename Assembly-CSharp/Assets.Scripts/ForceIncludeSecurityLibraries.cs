using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

namespace Assets.Scripts;

public class ForceIncludeSecurityLibraries : MonoBehaviour
{
	private void Start()
	{
		try
		{
			SHA1Managed sHA1Managed = new SHA1Managed();
			sHA1Managed.ComputeHash((Stream)null);
			sHA1Managed.ComputeHash(new byte[0]);
			ZipFile zipFile = new ZipFile("Hack = not epic");
			ZipEntry entry = zipFile.GetEntry("YEEEEET");
			zipFile.GetInputStream(entry);
			AndroidJavaObject androidJavaObject = new AndroidJavaClass("").CallStatic<AndroidJavaObject>("null", new object[0]);
			androidJavaObject.Call<string>("yeetus", new object[0]);
			int num = ((Stream)null).Read((byte[])null, 0, 0);
			base.name = zipFile.ToString();
			base.name = entry.Name;
			base.name = androidJavaObject.ToString();
			base.name = androidJavaObject.ToString();
			base.name = num.ToString();
			base.name = ((object)1 == (object)2).ToString();
			base.name = Encoding.UTF8.GetBytes("fetus deletus").ToString();
			base.name = BitConverter.ToString(new byte[0]);
			base.name = Path.GetFileName("Nice ....");
			AndroidJNI.AttachCurrentThread();
			Path.GetDirectoryName("");
			Directory.GetFiles("");
			Directory.Exists("");
			Directory.GetDirectories("");
		}
		catch
		{
		}
	}

	private void Test()
	{
		Debug.Log(typeof(Directory).AssemblyQualifiedName);
	}
}
