using System;
using System.Collections.Generic;
using System.Globalization;
using Beebyte.Obfuscator;
using SFS.Logs;
using SFS.UI;
using SFS.World;
using SFS.World.Drag;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.Stats;

public class StatsRecorder : MonoBehaviour
{
	public class Tracker
	{
		[Skip]
		public enum State_Orbit
		{
			None,
			Sub,
			Low,
			Trans,
			High,
			Esc
		}

		[Skip]
		public enum State_Atmosphere
		{
			Lower,
			Upper,
			Outside
		}

		private StatsRecorder owner;

		public bool state_Landed;

		public double state_Height;

		public State_Orbit state_Orbit;

		public State_Orbit state_Orbit_Old;

		public State_Atmosphere state_Atmosphere;

		public List<string> lastReentryEvent;

		private List<string> lastCrashEvent;

		public Tracker(bool state_Landed, double state_Height, State_Orbit state_Orbit, State_Orbit state_Orbit_Old, State_Atmosphere state_Atmosphere, List<string> lastReentryEvent, List<string> lastCrashEvent, StatsRecorder owner)
		{
			this.state_Landed = state_Landed;
			this.state_Height = state_Height;
			this.state_Orbit = state_Orbit;
			this.state_Orbit_Old = state_Orbit_Old;
			this.state_Atmosphere = state_Atmosphere;
			this.lastReentryEvent = lastReentryEvent;
			this.lastCrashEvent = lastCrashEvent;
			this.owner = owner;
		}

		public void Record_Landed(Location location)
		{
			bool flag = ((!state_Landed) ? location.velocity.Mag_LessThan(0.001) : (location.velocity.Mag_LessThan(2.0) || location.TerrainHeight < 300.0));
			if (flag != state_Landed)
			{
				state_Landed = flag;
				owner.RecordEvent(Log_Landed(flag, location.position.AngleDegrees));
				if ((bool)owner.player.isPlayer)
				{
					LogsModule.Log_Landed(ShowMsg_Big, state_Landed, owner.location.planet.Value, location.position.AngleDegrees, endMissionMenu: false);
				}
			}
		}

		public void Record_Height(Location location)
		{
			double height = location.Height;
			if (location.planet == Base.planetLoader.spaceCenter.Planet)
			{
				int[] altitudeMilestones = Base.worldBase.settings.difficulty.AltitudeMilestones;
				foreach (int num in altitudeMilestones)
				{
					if (state_Height <= (double)num && height > (double)num)
					{
						owner.RecordEvent(Log_Height(num));
						if ((bool)owner.player.isPlayer)
						{
							LogsModule.Log_ReachedHeight(ShowMsg_Big, num);
						}
					}
				}
			}
			state_Height = height;
		}

		public void Record_Orbit(Location location)
		{
			State_Orbit state = GetState(location, owner.tracker.state_Landed);
			if (state != state_Orbit)
			{
				owner.RecordEvent(Log_Orbit(state));
				if ((bool)owner.player.isPlayer)
				{
					LogsModule.Log_Orbit(ShowMsg_Big, owner.location.planet.Value, state, state_Orbit, state_Orbit_Old);
				}
				state_Orbit_Old = state_Orbit;
				state_Orbit = state;
			}
		}

		public static State_Orbit GetState(Location location, bool landed)
		{
			Orbit orbit = Orbit.TryCreateOrbit(location, calculateTimeParameters: false, calculateEncounters: false, out var success);
			if (!success | landed)
			{
				return State_Orbit.None;
			}
			if (orbit.apoapsis > location.planet.SOI)
			{
				return State_Orbit.Esc;
			}
			if (orbit.periapsis < location.planet.OrbitRadius)
			{
				return State_Orbit.Sub;
			}
			if (orbit.periapsis > location.planet.OrbitRadius * 2.5)
			{
				return State_Orbit.High;
			}
			if (orbit.apoapsis > location.planet.OrbitRadius * 1.5)
			{
				return State_Orbit.Trans;
			}
			return State_Orbit.Low;
		}

		public static State_Orbit GetStartState()
		{
			return State_Orbit.Sub;
		}

		public void Record_Atmosphere(Location location)
		{
			State_Atmosphere state = GetState(location);
			if (state != state_Atmosphere)
			{
				owner.RecordEvent(Log_Atmosphere(state));
				if ((bool)owner.player.isPlayer)
				{
					LogsModule.Log_Atmosphere(ShowMsg_Big, owner.location.planet.Value, state, state_Atmosphere, hideEnteredAtmosphere: false);
				}
				state_Atmosphere = state;
			}
		}

		public static State_Atmosphere GetState(Location location)
		{
			if (!location.planet.HasAtmospherePhysics)
			{
				return State_Atmosphere.Outside;
			}
			double height = location.Height;
			if (height > location.planet.AtmosphereHeightPhysics)
			{
				return State_Atmosphere.Outside;
			}
			if (height > location.planet.AtmosphereHeightPhysics * location.planet.data.atmospherePhysics.upperAtmosphere)
			{
				return State_Atmosphere.Upper;
			}
			return State_Atmosphere.Lower;
		}

		public void Record_Reentry(Location location, Player player)
		{
			float num = 0f;
			if (AeroModule.IsInsideAtmosphereAndIsMoving(location))
			{
				HeatModuleBase[] componentsInChildren = player.GetComponentsInChildren<HeatModuleBase>();
				foreach (HeatModuleBase heatModuleBase in componentsInChildren)
				{
					if (!float.IsInfinity(heatModuleBase.Temperature))
					{
						num = Mathf.Max(num, heatModuleBase.Temperature);
					}
				}
			}
			else
			{
				num = 0f;
			}
			if (num > 100f && location.VerticalVelocity < 0.0)
			{
				if (lastReentryEvent == null)
				{
					lastReentryEvent = Log_Reentry(num);
					owner.RecordEvent(lastReentryEvent);
				}
				if (num > float.Parse(lastReentryEvent[2], CultureInfo.InvariantCulture))
				{
					lastReentryEvent[2] = num.ToString(CultureInfo.InvariantCulture);
				}
			}
			else if (num < 10f && lastReentryEvent != null)
			{
				lastReentryEvent = null;
			}
		}

		public void NewReentryEvent()
		{
			lastReentryEvent = null;
		}

		public void OnCrash()
		{
			if (lastCrashEvent == null || WorldTime.main.worldTime - double.Parse(lastCrashEvent[3], CultureInfo.InvariantCulture) > 10.0)
			{
				lastCrashEvent = Log_Crash(1, WorldTime.main.worldTime);
				owner.RecordEvent(lastCrashEvent);
			}
			else
			{
				lastCrashEvent[2] = (int.Parse(lastCrashEvent[2], CultureInfo.InvariantCulture) + 1).ToString();
				lastCrashEvent[3] = WorldTime.main.worldTime.ToString(CultureInfo.InvariantCulture);
			}
			if ((bool)owner.player.isPlayer)
			{
				LogsModule.Log_Crash(ShowMsg_Big, owner.location.planet.Value);
			}
		}

		public void NewCrashEvent()
		{
			lastCrashEvent = null;
		}
	}

	[Serializable]
	public enum EventType
	{
		Landed,
		Height,
		Orbit,
		Atmosphere,
		Reentry,
		Planet,
		LeftCapsule,
		Flag,
		CollectRock,
		Crash
	}

	private const float RecordTime = 1f;

	public WorldLocation location;

	public Player player;

	public int branch = -1;

	private Location location_Old;

	public Tracker tracker;

	public ChallengeRecorder challengeRecorder;

	private static string Time => WorldTime.main.worldTime.Round(1).ToString();

	public void Load(int branch)
	{
		if (!LogManager.main.branches.ContainsKey(branch))
		{
			LogManager.main.CreateRoot(out branch);
			Location value = location.Value;
			bool landed = value.velocity.Mag_LessThan(1.0);
			Initialize(branch, landed, value.Height, Tracker.GetState(value, landed), Tracker.GetState(value, landed), Tracker.GetState(value), null, null, new Dictionary<Challenge, (int, string)>(), new HashSet<Challenge>());
			return;
		}
		Dictionary<int, Branch> branches = LogManager.main.branches;
		List<string> list = GetLastEvent(EventType.Landed);
		List<string> list2 = GetLastEvent(EventType.Orbit);
		List<string> list3 = GetLastEvent(EventType.Orbit, skipOne: true);
		List<string> list4 = GetLastEvent(EventType.Atmosphere);
		List<string> reentry = GetLastEvent(EventType.Reentry);
		List<string> lastCrashEvent = GetLastEvent(EventType.Crash);
		bool landed2 = list != null && bool.Parse(list[2]);
		Tracker.State_Orbit orbit = ((list2 != null) ? ((Tracker.State_Orbit)Enum.Parse(typeof(Tracker.State_Orbit), list2[2])) : Tracker.GetStartState());
		Tracker.State_Orbit orbit_Old = ((list3 != null) ? ((Tracker.State_Orbit)Enum.Parse(typeof(Tracker.State_Orbit), list3[2])) : Tracker.GetStartState());
		Tracker.State_Atmosphere atmosphere = ((list4 != null) ? ((Tracker.State_Atmosphere)Enum.Parse(typeof(Tracker.State_Atmosphere), list4[2])) : Tracker.GetState(location.Value));
		Dictionary<Challenge, (int i, string progressData)> progress = new Dictionary<Challenge, (int, string)>();
		HashSet<Challenge> completed = new HashSet<Challenge>();
		HashSet<int> traversed = new HashSet<int>();
		SearchBranch(branch);
		Initialize(branch, landed2, location.Value.Height, orbit, orbit_Old, atmosphere, reentry, lastCrashEvent, progress, completed);
		List<string> GetLastEvent(EventType eventType, bool skipOne = false)
		{
			string eventName = eventType.ToString();
			return SearchBranch2(branch);
			List<string> SearchBranch2(int b)
			{
				if (!branches.ContainsKey(b))
				{
					return null;
				}
				Branch branch2 = branches[b];
				for (int num = branch2.logEvents.Count - 1; num >= 0; num--)
				{
					if (branch2.logEvents[num][0] == eventName)
					{
						if (!skipOne)
						{
							return branch2.logEvents[num];
						}
						skipOne = false;
					}
				}
				return SearchBranch2(branch2.parentA);
			}
		}
		void SearchBranch(int b)
		{
			Branch branch2 = branches[b];
			for (int num = branch2.challengeEvents.Count - 1; num >= 0; num--)
			{
				string[] array = branch2.challengeEvents[num].Split(',', StringSplitOptions.None);
				Challenge challenge = Base.worldBase.challenges[array[0]];
				if (!completed.Contains(challenge))
				{
					int num2 = int.Parse(array[1]);
					if (num2 >= challenge.steps.Count - 1 && array.Length < 3)
					{
						completed.Add(challenge);
						progress.Remove(challenge);
					}
					else
					{
						(int, string) value2 = (num2, array.IsValidIndex(2) ? array[2] : null);
						if (!progress.TryGetValue(challenge, out (int, string) value3) || num2 > value3.Item1)
						{
							progress[challenge] = value2;
						}
						else if (num2 == value3.Item1)
						{
							value2.Item2 = challenge.steps[num2].OnConflict(value3.Item2, value2.Item2);
							progress[challenge] = value2;
						}
					}
				}
			}
			traversed.Add(b);
			if (branches.ContainsKey(branch2.parentA) && !traversed.Contains(branch2.parentA))
			{
				SearchBranch(branch2.parentA);
			}
			if (branches.ContainsKey(branch2.parentB) && !traversed.Contains(branch2.parentB))
			{
				SearchBranch(branch2.parentB);
			}
		}
	}

	public static void OnSplit(StatsRecorder A, StatsRecorder B)
	{
		int oldBranch = A.branch;
		LogManager.main.SplitBranch(oldBranch, out var branch_A, out var branch_B);
		A.branch = branch_A;
		var (lastReentryEvent, reentry) = SplitReentry();
		A.tracker.lastReentryEvent = lastReentryEvent;
		A.tracker.NewCrashEvent();
		A.challengeRecorder.Split(out Dictionary<Challenge, (int, string)> progress, out HashSet<Challenge> complete);
		B.Initialize(branch_B, A.tracker.state_Landed, A.tracker.state_Height, A.tracker.state_Orbit, A.tracker.state_Orbit_Old, A.tracker.state_Atmosphere, reentry, null, progress, complete);
		(List<string>, List<string>) SplitReentry()
		{
			List<string> lastReentryEvent2 = A.tracker.lastReentryEvent;
			if (lastReentryEvent2 == null)
			{
				return (null, null);
			}
			LogManager.main.branches[oldBranch].logEvents.Remove(lastReentryEvent2);
			float num = float.Parse(lastReentryEvent2[2], CultureInfo.InvariantCulture);
			List<string> list = Log_Reentry(num);
			List<string> list2 = Log_Reentry(num);
			LogManager.main.branches[branch_A].AddEvent_Log(list);
			LogManager.main.branches[branch_B].AddEvent_Log(list2);
			return (list, list2);
		}
	}

	public static void OnMerge(StatsRecorder A, StatsRecorder B)
	{
		LogManager.main.MergeBranch(A.branch, B.branch, out var newBranch);
		A.branch = newBranch;
		A.tracker.NewCrashEvent();
		A.tracker.NewReentryEvent();
		if (B.tracker.state_Landed != A.tracker.state_Landed)
		{
			B.RecordEvent(Log_Landed(A.tracker.state_Landed, B.location.position.Value.AngleDegrees));
		}
		if (B.tracker.state_Orbit != A.tracker.state_Orbit)
		{
			B.RecordEvent(Log_Orbit(A.tracker.state_Orbit));
		}
		if (B.tracker.state_Atmosphere != A.tracker.state_Atmosphere)
		{
			B.RecordEvent(Log_Atmosphere(A.tracker.state_Atmosphere));
		}
		A.challengeRecorder.Merge(B.challengeRecorder);
		if ((bool)A.player.isPlayer)
		{
			LogsModule.Log_Dock(ShowMsg_Big, A.location.planet, A.tracker.state_Landed, A.tracker.state_Orbit);
		}
	}

	private void OnPlanetChange(Planet oldPlanet, Planet newPlanet)
	{
		if (!(oldPlanet == null) && !(newPlanet == null) && !(oldPlanet == newPlanet))
		{
			RecordEvent(Log_Planet(newPlanet.codeName, location.position.Value.AngleDegrees, location.velocity.Value));
			if ((bool)player.isPlayer)
			{
				LogsModule.Log_Planet(ShowMsg_Big, newPlanet, oldPlanet);
			}
			tracker.state_Landed = false;
			tracker.state_Height = double.PositiveInfinity;
			tracker.state_Orbit = Tracker.State_Orbit.Sub;
			tracker.state_Orbit_Old = Tracker.State_Orbit.Sub;
			tracker.state_Atmosphere = Tracker.State_Atmosphere.Outside;
			tracker.NewReentryEvent();
			tracker.NewCrashEvent();
			challengeRecorder.UpdateEligibleSteps();
		}
	}

	private void Initialize(int branch, bool landed, double height, Tracker.State_Orbit orbit, Tracker.State_Orbit orbit_Old, Tracker.State_Atmosphere atmosphere, List<string> reentry, List<string> lastCrashEvent, Dictionary<Challenge, (int i, string progressData)> progress, HashSet<Challenge> complete)
	{
		this.branch = branch;
		tracker = new Tracker(landed, height, orbit, orbit_Old, atmosphere, reentry, lastCrashEvent, this);
		challengeRecorder = new ChallengeRecorder(progress, complete, this);
	}

	private void Start()
	{
		InvokeRepeating("Record", UnityEngine.Random.Range(0.5f, 1.5f), 1f);
		location.planet.OnChange += new Action<Planet, Planet>(OnPlanetChange);
	}

	private void Record()
	{
		Location value = location.Value;
		tracker.Record_Landed(value);
		tracker.Record_Height(value);
		tracker.Record_Orbit(value);
		tracker.Record_Atmosphere(value);
		tracker.Record_Reentry(value, player);
		challengeRecorder.TryCompleteSteps(value);
	}

	public void OnLeaveCapsule(string astronautName)
	{
		RecordEvent(Log_LeftCapsule(astronautName));
	}

	public void OnPlantFlag(double angleDegrees)
	{
		RecordEvent(Log_Flag(angleDegrees));
		LogsModule.Log_Flag(ShowMsg_Small, location.planet.Value, angleDegrees);
	}

	public void OnCollectRock(double angleDegrees)
	{
		RecordEvent(Log_CollectRock(angleDegrees));
		LogsModule.Log_CollectRock(ShowMsg_Small, location.planet.Value, angleDegrees);
	}

	public void OnCrash(float impactVelocity)
	{
		tracker.OnCrash();
		challengeRecorder.OnCrash(impactVelocity);
	}

	private void RecordEvent(List<string> data)
	{
		LogManager.main.branches[branch].AddEvent_Log(data);
	}

	private static void ShowMsg_Small(LogId id, double reward, string msg)
	{
		MsgDrawer.main.Log(msg);
	}

	private static void ShowMsg_Big(LogId id, double reward, string msg)
	{
		MsgDrawer.main.Log(msg, big: true);
	}

	public bool HasFlown()
	{
		List<int> list = new List<int>();
		int num = branch;
		while (true)
		{
			if (list.Count > 1000)
			{
				MsgDrawer.main.Log("LOOP");
				return false;
			}
			list.Add(num);
			int parentA = LogManager.main.branches[num].parentA;
			if (parentA == -1)
			{
				break;
			}
			num = parentA;
		}
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			foreach (List<string> logEvent in LogManager.main.branches[list[num2]].logEvents)
			{
				if (logEvent[0] == EventType.Landed.ToString() && logEvent[2] == false.ToString())
				{
					return true;
				}
			}
		}
		return false;
	}

	private static List<string> Log_Landed(bool landed, double angle)
	{
		return new List<string>
		{
			EventType.Landed.ToString(),
			Time,
			landed.ToString(),
			angle.ToString()
		};
	}

	private static List<string> Log_Height(int height)
	{
		return new List<string>
		{
			EventType.Height.ToString(),
			Time,
			height.ToString()
		};
	}

	private static List<string> Log_Orbit(Tracker.State_Orbit state)
	{
		return new List<string>
		{
			EventType.Orbit.ToString(),
			Time,
			state.ToString()
		};
	}

	private static List<string> Log_Atmosphere(Tracker.State_Atmosphere state_Atmosphere)
	{
		return new List<string>
		{
			EventType.Atmosphere.ToString(),
			Time,
			state_Atmosphere.ToString()
		};
	}

	private static List<string> Log_Reentry(double maxTemp)
	{
		return new List<string>
		{
			EventType.Reentry.ToString(),
			Time,
			maxTemp.ToString(CultureInfo.InvariantCulture)
		};
	}

	private static List<string> Log_Planet(string planet, double angle, Double2 velocity)
	{
		return new List<string>
		{
			EventType.Planet.ToString(),
			Time,
			planet,
			angle.ToString(),
			velocity.x.ToString(),
			velocity.y.ToString()
		};
	}

	private static List<string> Log_LeftCapsule(string astronautName)
	{
		return new List<string>
		{
			EventType.LeftCapsule.ToString(),
			Time,
			astronautName
		};
	}

	private static List<string> Log_Flag(double angle)
	{
		return new List<string>
		{
			EventType.Flag.ToString(),
			Time,
			angle.ToString()
		};
	}

	private static List<string> Log_CollectRock(double angle)
	{
		return new List<string>
		{
			EventType.CollectRock.ToString(),
			Time,
			angle.ToString()
		};
	}

	private static List<string> Log_Crash(int count, double lastCrashTime)
	{
		return new List<string>
		{
			EventType.Crash.ToString(),
			Time,
			count.ToString(),
			lastCrashTime.ToString()
		};
	}
}
