using System;
using SFS.IO;
using SFS.Parsers.Json;
using UnityEngine;

public class TimeTracker : MonoBehaviour
{
	[Serializable]
	public class Data
	{
		public int gameTimeSeconds;

		public long realTimeSeconds;

		public int sessionCount;
	}

	private int sessionPlaytime = 10;

	private DateTime lastFrameTime = DateTime.MinValue;

	private static FilePath PlaytimePath => FileLocations.GetNotificationsPath("TimeTracker/Playtime");

	public static Data GetPlaytime()
	{
		return GetData(PlaytimePath);
	}

	public static void Write(string id, bool onlyFirst = false)
	{
		FilePath pathFirst = GetPathFirst(id);
		bool flag = pathFirst.FileExists();
		if (!flag || !onlyFirst)
		{
			Data data = GetData(PlaytimePath);
			data.realTimeSeconds = DateTime.Now.Ticks / 10000000;
			if (!flag)
			{
				WriteData(pathFirst, data);
			}
			if (!onlyFirst)
			{
				WriteData(GetPathLast(id), data);
			}
		}
	}

	public static bool GetHasHappen(string id)
	{
		return GetPathFirst(id).FileExists();
	}

	public static Data GetTimeSince_First(string id)
	{
		return GetTimeSince(GetPathFirst(id));
	}

	public static Data GetTimeSince_Last(string id)
	{
		return GetTimeSince(GetPathLast(id));
	}

	private static Data GetTimeSince(FilePath path)
	{
		if (!TryGetData(path, out var data))
		{
			return new Data
			{
				gameTimeSeconds = 0,
				realTimeSeconds = 0L,
				sessionCount = 0
			};
		}
		Data data2 = GetData(PlaytimePath);
		data2.gameTimeSeconds -= data.gameTimeSeconds;
		data2.realTimeSeconds -= DateTime.Now.Ticks / 10000000 - data.realTimeSeconds;
		data2.sessionCount -= data.sessionCount;
		return data;
	}

	private static FilePath GetPathFirst(string id)
	{
		return FileLocations.GetNotificationsPath("TimeTracker/First_" + id);
	}

	private static FilePath GetPathLast(string id)
	{
		return FileLocations.GetNotificationsPath("TimeTracker/Last_" + id);
	}

	private void Update()
	{
		if (Time.unscaledTime > (float)sessionPlaytime)
		{
			sessionPlaytime += 10;
			Increase(delegate(Data a)
			{
				a.gameTimeSeconds += 10;
			});
		}
		if ((DateTime.Now - lastFrameTime).Seconds > 300)
		{
			Increase(delegate(Data a)
			{
				a.sessionCount++;
			});
		}
		lastFrameTime = DateTime.Now;
		static void Increase(Action<Data> a)
		{
			FilePath playtimePath = PlaytimePath;
			Data data = GetData(playtimePath);
			a(data);
			WriteData(playtimePath, data);
		}
	}

	private static void WriteData(FilePath path, Data data)
	{
		path.WriteText(JsonWrapper.ToJson(data, pretty: true));
	}

	private static Data GetData(FilePath path)
	{
		if (!TryGetData(path, out var data))
		{
			return new Data();
		}
		return data;
	}

	private static bool TryGetData(FilePath path, out Data data)
	{
		return JsonWrapper.TryLoadJson<Data>(path, out data);
	}
}
