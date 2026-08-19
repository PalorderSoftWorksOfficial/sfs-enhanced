using System;
using System.Collections.Generic;
using SFS.IO;
using UnityEngine;

public class ErrorLogger : MonoBehaviour
{
	private Queue<(string, string, LogType)> logs = new Queue<(string, string, LogType)>();

	private FilePath logLocation;

	private void Awake()
	{
		FolderPath logsFolder = FileLocations.LogsFolder;
		FilePath filePath = logsFolder.ExtendToFile("counter.txt");
		long num = 0L;
		if (filePath.FileExists())
		{
			try
			{
				num = long.Parse(filePath.ReadText()) % 5;
			}
			catch
			{
			}
		}
		filePath.WriteText((num + 1).ToString());
		foreach (FilePath item in logsFolder.GetFilesInFolder(recursively: false))
		{
			if (item.CleanFileName.StartsWith(num + "_"))
			{
				item.DeleteFile();
			}
		}
		logLocation = logsFolder.ExtendToFile(num + "_" + DateTime.Now.ToString("hh_mm__dd_MMMM_yyyy") + ".txt");
		Application.logMessageReceived += LogMessage;
		Application.logMessageReceivedThreaded += LogMessage;
	}

	private void Update()
	{
		lock (logs)
		{
			while (logs.Count > 0)
			{
				var (text, text2, logType) = logs.Dequeue();
				logLocation.AppendText("[" + DateTime.Now.ToString("hh:mm:ss") + " " + logType.ToString() + "]: " + text + "\n" + text2 + "\n");
			}
		}
	}

	private void LogMessage(string condition, string stackTrace, LogType type)
	{
		lock (logs)
		{
			logs.Enqueue((condition, stackTrace, type));
		}
	}
}
