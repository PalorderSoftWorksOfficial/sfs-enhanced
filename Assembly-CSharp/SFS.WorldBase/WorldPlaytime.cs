using System;

namespace SFS.WorldBase;

[Serializable]
public class WorldPlaytime
{
	public long lastPlayedTime_Ticks;

	public double totalPlayTime_Seconds;

	public WorldPlaytime()
	{
	}

	public WorldPlaytime(long lastPlayedTime_Ticks, double totalPlayTime_Seconds)
	{
		this.lastPlayedTime_Ticks = lastPlayedTime_Ticks;
		this.totalPlayTime_Seconds = totalPlayTime_Seconds;
	}
}
