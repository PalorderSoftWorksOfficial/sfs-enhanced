using System;

namespace SFS.Logs;

[Serializable]
public struct LogId(LogType type, int value, string planet)
{
	public LogType type = type;

	public int value = value;

	public string planet = planet;
}
