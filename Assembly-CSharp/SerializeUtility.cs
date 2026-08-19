using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class SerializeUtility
{
	private static JsonSerializer defaultSerializer;

	private static JsonSerializer DefaultSerializer => defaultSerializer ?? (defaultSerializer = JsonSerializer.CreateDefault());

	public static void SerializeTo(this JsonSerializer serializer, string path, object value, Type type)
	{
		StreamWriter streamWriter = new StreamWriter(path);
		serializer.Serialize(streamWriter, value, type);
		streamWriter.Close();
	}

	public static void Populate(this JObject jObject, object target, JsonSerializer serializer)
	{
		if (serializer == null)
		{
			serializer = DefaultSerializer;
		}
		serializer.Populate(jObject.CreateReader(), target);
	}
}
