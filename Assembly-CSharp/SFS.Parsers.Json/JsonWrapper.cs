using System;
using Newtonsoft.Json;
using SFS.IO;
using UnityEngine;

namespace SFS.Parsers.Json;

public static class JsonWrapper
{
	public class VersionedData<T>
	{
		public T data;
	}

	private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new MainContractResolver(),
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		NullValueHandling = NullValueHandling.Ignore
	};

	public static bool TryLoadJson<T>(FilePath file, out T data)
	{
		try
		{
			if (!file.FileExists())
			{
				data = default(T);
				return false;
			}
			string json = file.ReadText();
			data = FromJson<T>(json);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			data = default(T);
			return false;
		}
	}

	public static void SaveAsJson(FilePath file, object data, bool pretty)
	{
		if (!file.GetParent().FolderExists())
		{
			file.GetParent().CreateFolder();
		}
		file.WriteText(ToJson(data, pretty));
	}

	public static T FromJson<T>(string json)
	{
		try
		{
			VersionedData<T> versionedData = JsonConvert.DeserializeObject<VersionedData<T>>(json, SerializerSettings);
			if (versionedData.data != null)
			{
				return versionedData.data;
			}
		}
		catch
		{
		}
		return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
	}

	public static string ToJson(object data, bool pretty)
	{
		return JsonConvert.SerializeObject(data, (pretty || Application.isEditor) ? Formatting.Indented : Formatting.None, SerializerSettings);
	}
}
