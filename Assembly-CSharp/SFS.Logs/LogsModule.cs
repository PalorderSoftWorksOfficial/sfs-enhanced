using System;
using SFS.Stats;
using SFS.Translations;
using SFS.World;
using SFS.World.Maps;
using SFS.WorldBase;

namespace SFS.Logs;

[Serializable]
public class LogsModule
{
	public bool Landed = true;

	public bool Takeoff = true;

	public bool Atmosphere = true;

	public bool Orbit = true;

	public bool Crash = true;

	public static void Log_Dock(Action<LogId, double, string> logger, Planet planet, bool landed, StatsRecorder.Tracker.State_Orbit orbit)
	{
		if (landed)
		{
			double arg = ((planet.codeName == Base.planetLoader.spaceCenter.address) ? 0.0 : (15.0 * planet.RewardMultiplier));
			logger(new LogId(LogType.Dock, 0, planet.codeName), arg, Loc.main.Docked_Surface.InjectField(planet.DisplayName, "planet"));
			return;
		}
		Field field = orbit switch
		{
			StatsRecorder.Tracker.State_Orbit.Low => Loc.main.Docked_Orbit_Low, 
			StatsRecorder.Tracker.State_Orbit.High => Loc.main.Docked_Orbit_High, 
			StatsRecorder.Tracker.State_Orbit.Esc => Loc.main.Docked_Escape, 
			StatsRecorder.Tracker.State_Orbit.Trans => Loc.main.Docked_Orbit_Transfer, 
			_ => Loc.main.Docked_Suborbital, 
		};
		logger(new LogId(LogType.Dock, 1, planet.codeName), 10.0 * planet.RewardMultiplier, field.InjectField(planet.DisplayName, "planet"));
	}

	public static void Log_Orbit(Action<LogId, double, string> logger, Planet planet, StatsRecorder.Tracker.State_Orbit orbit, StatsRecorder.Tracker.State_Orbit orbit_Old, StatsRecorder.Tracker.State_Orbit orbit_Old_Old)
	{
		if (!planet.data.logs.Orbit)
		{
			return;
		}
		switch (orbit)
		{
		case StatsRecorder.Tracker.State_Orbit.Low:
			switch (orbit_Old)
			{
			case StatsRecorder.Tracker.State_Orbit.Sub:
			{
				bool flag = planet == Base.planetLoader.spaceCenter.Planet;
				Log(0, Loc.main.Reached_Low_Orbit, flag ? 60 : 20);
				break;
			}
			case StatsRecorder.Tracker.State_Orbit.Trans:
				if (orbit_Old_Old == StatsRecorder.Tracker.State_Orbit.Esc)
				{
					Log(1, Loc.main.Capture_Low_Orbit, 20.0);
				}
				else
				{
					Log(1, Loc.main.Descend_Low_Orbit, 20.0);
				}
				break;
			}
			break;
		case StatsRecorder.Tracker.State_Orbit.High:
			switch (orbit_Old)
			{
			case StatsRecorder.Tracker.State_Orbit.Trans:
				Log(3, Loc.main.Reached_High_Orbit, 25.0);
				break;
			case StatsRecorder.Tracker.State_Orbit.Esc:
				Log(3, Loc.main.Capture_High_Orbit, 15.0);
				break;
			}
			break;
		}
		void Log(int valueId, Field text, double reward)
		{
			logger(new LogId(LogType.Orbit, valueId, planet.codeName), reward * planet.RewardMultiplier, text.InjectField(planet.DisplayName, "planet"));
		}
	}

	public static void Log_Landed(Action<LogId, double, string> logger, bool landed, Planet planet, double angleDegrees, bool endMissionMenu)
	{
		if (!landed || !planet.data.logs.Landed)
		{
			return;
		}
		Landmark landmark = null;
		double num = -1000.0;
		if (!double.IsNaN(angleDegrees))
		{
			Landmark[] landmarks = planet.landmarks;
			foreach (Landmark landmark2 in landmarks)
			{
				double num2 = (0.0 - Math.Abs(Math_Utility.NormalizeAngleDegrees((double)landmark2.data.Center - angleDegrees))) / ((double)landmark2.data.AngularWidth * 0.5);
				if (num2 > -1.1 && num2 > num)
				{
					landmark = landmark2;
					num = num2;
				}
			}
		}
		LogId arg = new LogId(LogType.Landed, 0, planet.codeName);
		double arg2 = 50.0 * planet.RewardMultiplier;
		if (landmark != null)
		{
			logger(arg, arg2, (endMissionMenu ? Loc.main.Landed_At_Landmark__Short : Loc.main.Landed_At_Landmark).InjectField(planet.DisplayName, "planet").InjectField(landmark.displayName, "landmark"));
		}
		else
		{
			logger(arg, arg2, Loc.main.Landed.InjectField(planet.DisplayName, "planet"));
		}
	}

	public static void Log_Planet(Action<LogId, double, string> logger, Planet planet, Planet planet_Old)
	{
		if (planet.parentBody == planet_Old)
		{
			logger(new LogId(LogType.Changed_SOI, 0, planet.codeName), 10.0, Loc.main.Entered_SOI.InjectField(planet.DisplayName, "planet"));
		}
		else
		{
			logger(new LogId(LogType.Changed_SOI, 1, planet_Old.codeName), 10.0, Loc.main.Escaped_SOI.InjectField(planet_Old.DisplayName, "planet"));
		}
	}

	public static void Log_Crash(Action<LogId, double, string> logger, Planet planet)
	{
		if (planet.data.logs.Crash)
		{
			logger(new LogId(LogType.Crash, 0, planet.codeName), 0.0, Loc.main.Crashed_Into_Terrain.InjectField(planet.DisplayName, "planet"));
		}
	}

	public static void Log_LeftCapsule(Action<LogId, double, string> logger, Planet planet, bool landed, StatsRecorder.Tracker.State_Orbit orbit)
	{
		if (landed)
		{
			double arg = ((planet.codeName == Base.planetLoader.spaceCenter.address) ? 0.0 : (10.0 * planet.RewardMultiplier));
			logger(new LogId(LogType.LeftCapsule, 0, planet.codeName), arg, Loc.main.EVA_Surface.InjectField(planet.DisplayName, "planet"));
			return;
		}
		(int, Field, int) tuple = orbit switch
		{
			StatsRecorder.Tracker.State_Orbit.Low => (1, Loc.main.EVA_Orbit_Low, 0), 
			StatsRecorder.Tracker.State_Orbit.High => (2, Loc.main.EVA_Orbit_High, 10), 
			StatsRecorder.Tracker.State_Orbit.Esc => (3, Loc.main.EVA_Escape, 0), 
			StatsRecorder.Tracker.State_Orbit.Trans => (4, Loc.main.EVA_Orbit_Transfer, 0), 
			_ => (5, Loc.main.EVA_Suborbital, 0), 
		};
		(int, Field, double) tuple2 = (tuple.Item1, tuple.Item2, tuple.Item3);
		logger(new LogId(LogType.LeftCapsule, tuple2.Item1, planet.codeName), tuple2.Item3 * planet.RewardMultiplier, tuple2.Item2.InjectField(planet.DisplayName, "planet"));
	}

	public static void Log_Flag(Action<LogId, double, string> logger, Planet planet, double angleDegrees)
	{
		double arg = ((planet.codeName == Base.planetLoader.spaceCenter.address) ? 0.0 : (10.0 * planet.RewardMultiplier));
		logger(new LogId(LogType.Flag, 0, planet.codeName), arg, Loc.main.Planted_Flag.InjectField(planet.DisplayName, "planet"));
	}

	public static void Log_CollectRock(Action<LogId, double, string> logger, Planet planet, double angleDegrees)
	{
		double arg = ((planet.codeName == Base.planetLoader.spaceCenter.address) ? 0.0 : (10.0 * planet.RewardMultiplier));
		logger(new LogId(LogType.CollectedRock, 0, planet.codeName), arg, Loc.main.Collected_Rock.InjectField(planet.DisplayName, "planet"));
	}

	public static void Log_Atmosphere(Action<LogId, double, string> logger, Planet planet, StatsRecorder.Tracker.State_Atmosphere state, StatsRecorder.Tracker.State_Atmosphere state_Old, bool hideEnteredAtmosphere)
	{
		if (!planet.data.logs.Atmosphere)
		{
			return;
		}
		if (state == StatsRecorder.Tracker.State_Atmosphere.Lower && state_Old == StatsRecorder.Tracker.State_Atmosphere.Upper)
		{
			if (!hideEnteredAtmosphere)
			{
				logger(new LogId(LogType.Atmosphere, 0, planet.codeName), 0.0, Loc.main.Entered_Lower_Atmosphere.InjectField(planet.DisplayName, "planet"));
			}
			return;
		}
		switch (state)
		{
		case StatsRecorder.Tracker.State_Atmosphere.Upper:
			switch (state_Old)
			{
			case StatsRecorder.Tracker.State_Atmosphere.Outside:
				if (!hideEnteredAtmosphere)
				{
					logger(new LogId(LogType.Atmosphere, 1, planet.codeName), 0.0, Loc.main.Entered_Upper_Atmosphere.InjectField(planet.DisplayName, "planet"));
				}
				break;
			case StatsRecorder.Tracker.State_Atmosphere.Lower:
				if (planet != Base.planetLoader.spaceCenter.Planet)
				{
					logger(new LogId(LogType.Atmosphere, 2, planet.codeName), 0.0, Loc.main.Left_Lower_Atmosphere.InjectField(planet.DisplayName, "planet"));
				}
				break;
			}
			break;
		case StatsRecorder.Tracker.State_Atmosphere.Outside:
			if (state_Old == StatsRecorder.Tracker.State_Atmosphere.Upper)
			{
				bool flag = planet == Base.planetLoader.spaceCenter.Planet;
				string arg = (flag ? ((string)Loc.main.Reached_Karman_Line) : ((string)Loc.main.Left_Upper_Atmosphere.InjectField(planet.DisplayName, "planet")));
				logger(new LogId(LogType.Atmosphere, 3, planet.codeName), flag ? 50 : 15, arg);
			}
			break;
		}
	}

	public static void Log_End(Action<LogId, double, string> logger, Location location, bool landed)
	{
		Planet planet = location.planet;
		if (landed && planet.codeName == Base.planetLoader.spaceCenter.address)
		{
			logger(default(LogId), 0.0, Loc.main.Recover_Home.InjectField(planet.DisplayName, "planet"));
		}
	}

	public static void Log_ReachedHeight(Action<LogId, double, string> logger, double height)
	{
		int num = ((height == 15000.0) ? 20 : ((height == 10000.0) ? 15 : 10));
		logger(new LogId(LogType.Height, (int)height, null), num, Loc.main.Reached_Height.Inject((height > 2000.0) ? ((int)height).ToKmString() : height.ToDistanceString(decimals: false), "height"));
	}

	public static void Log_Reentry(Action<LogId, double, string> logger, Planet planet, float temperature)
	{
		logger(new LogId(LogType.Reentry, (int)temperature, planet.codeName), 0.0, Loc.main.Survived_Reentry.InjectField(planet.DisplayName, "planet").Inject(temperature.ToTemperatureString(), "temperature"));
	}
}
