using System;
using System.Collections.Generic;
using System.Globalization;
using SFS.Logs;
using SFS.Stats;
using SFS.WorldBase;

namespace SFS.World;

public class State
{
	private double startTime;

	public List<(string, double, LogId)> logs;

	public bool landed = true;

	private Planet planet = Base.planetLoader.spaceCenter.Planet;

	private StatsRecorder.Tracker.State_Orbit orbit = StatsRecorder.Tracker.GetStartState();

	private StatsRecorder.Tracker.State_Orbit orbit_Old = StatsRecorder.Tracker.GetStartState();

	private StatsRecorder.Tracker.State_Atmosphere atmosphere = StatsRecorder.Tracker.GetState(new Location(Base.planetLoader.spaceCenter.Planet, Base.planetLoader.spaceCenter.LaunchPadLocation.position));

	public State(List<(string, double, LogId)> logs, double startTime)
	{
		this.logs = logs;
		this.startTime = startTime;
	}

	public static State Merge(State a, State b)
	{
		if (a.startTime > b.startTime)
		{
			State state = b;
			State state2 = a;
			a = state;
			b = state2;
		}
		a.logs.AddRange(b.logs);
		return a;
	}

	public State CopyState(double launchTime)
	{
		return new State(new List<(string, double, LogId)>(), launchTime)
		{
			planet = planet,
			landed = landed,
			orbit = orbit,
			orbit_Old = orbit_Old,
			atmosphere = atmosphere
		};
	}

	public void ReplayLog(int branchId, ref bool space, ref bool LEO, ref bool moonLand)
	{
		foreach (List<string> logEvent in LogManager.main.branches[branchId].logEvents)
		{
			if (!Enum.TryParse<StatsRecorder.EventType>(logEvent[0], out var result))
			{
				continue;
			}
			switch (result)
			{
			case StatsRecorder.EventType.Planet:
			{
				Planet planet = logEvent[2].GetPlanet();
				LogsModule.Log_Planet(Log, planet, this.planet);
				landed = false;
				orbit = StatsRecorder.Tracker.State_Orbit.Sub;
				orbit_Old = StatsRecorder.Tracker.State_Orbit.Sub;
				atmosphere = StatsRecorder.Tracker.State_Atmosphere.Outside;
				this.planet = planet;
				break;
			}
			case StatsRecorder.EventType.Landed:
			{
				landed = bool.Parse(logEvent[2]);
				bool flag = double.TryParse((logEvent.Count > 3) ? logEvent[3] : null, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2);
				LogsModule.Log_Landed(Log, landed, this.planet, flag ? result2 : double.NaN, endMissionMenu: true);
				if (landed && this.planet.codeName == "Moon")
				{
					moonLand = true;
				}
				break;
			}
			case StatsRecorder.EventType.Orbit:
			{
				StatsRecorder.Tracker.State_Orbit state_Orbit = (StatsRecorder.Tracker.State_Orbit)Enum.Parse(typeof(StatsRecorder.Tracker.State_Orbit), logEvent[2]);
				LogsModule.Log_Orbit(Log, this.planet, state_Orbit, orbit, orbit_Old);
				orbit_Old = orbit;
				orbit = state_Orbit;
				if (orbit == StatsRecorder.Tracker.State_Orbit.Low && this.planet.codeName == "Earth")
				{
					LEO = true;
				}
				break;
			}
			case StatsRecorder.EventType.Atmosphere:
			{
				StatsRecorder.Tracker.State_Atmosphere state_Atmosphere = (StatsRecorder.Tracker.State_Atmosphere)Enum.Parse(typeof(StatsRecorder.Tracker.State_Atmosphere), logEvent[2]);
				LogsModule.Log_Atmosphere(Log, this.planet, state_Atmosphere, atmosphere, this.planet.codeName == Base.planetLoader.spaceCenter.address);
				atmosphere = state_Atmosphere;
				if (state_Atmosphere == StatsRecorder.Tracker.State_Atmosphere.Outside && this.planet.codeName == "Earth")
				{
					space = true;
				}
				break;
			}
			case StatsRecorder.EventType.Height:
				LogsModule.Log_ReachedHeight(Log, double.Parse(logEvent[2], CultureInfo.InvariantCulture));
				break;
			case StatsRecorder.EventType.Reentry:
				LogsModule.Log_Reentry(Log, this.planet, float.Parse(logEvent[2], CultureInfo.InvariantCulture));
				break;
			case StatsRecorder.EventType.LeftCapsule:
				LogsModule.Log_LeftCapsule(Log, this.planet, landed, orbit);
				break;
			case StatsRecorder.EventType.Flag:
				LogsModule.Log_Flag(Log, this.planet, double.Parse(logEvent[2]));
				break;
			case StatsRecorder.EventType.CollectRock:
				LogsModule.Log_CollectRock(Log, this.planet, double.Parse(logEvent[2]));
				break;
			case StatsRecorder.EventType.Crash:
				LogsModule.Log_Crash(Log, this.planet);
				break;
			}
		}
	}

	public void Log_Dock()
	{
		LogsModule.Log_Dock(Log, planet, landed, orbit);
	}

	public void Log(LogId id, double reward, string msg)
	{
		logs.Add((msg, reward, id));
	}
}
