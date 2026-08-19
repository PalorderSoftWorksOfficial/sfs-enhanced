using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SFS.Builds;
using SFS.IO;
using SFS.Stats;
using SFS.UI;
using SFS.World;
using UnityEngine;

namespace SFS.WorldBase;

public class SavingCache : MonoBehaviour
{
	public class Data<T>
	{
		public Thread thread;

		public (bool success, T data, string log) result;

		public static Data<T> Cache(T data, bool cache)
		{
			if (!cache)
			{
				return null;
			}
			return new Data<T>
			{
				result = (success: true, data: data, log: null)
			};
		}

		public void JoinThread()
		{
			if (thread != null && thread.IsAlive)
			{
				thread.Join();
			}
		}
	}

	public static SavingCache main;

	private Data<Blueprint> buildPersistent;

	private WorldSave revertToLaunch;

	private Data<WorldSave> worldPersistent;

	private void Awake()
	{
		main = this;
	}

	public void SaveBuildPersistent(Blueprint new_BuildPersistent, bool cache)
	{
		buildPersistent = Data<Blueprint>.Cache(new_BuildPersistent, cache);
		string version = Application.version;
		FolderPath path = Base.worldBase.paths.buildPersistentPath;
		SaveAsync(delegate
		{
			Blueprint.Save(path, new_BuildPersistent, version);
		});
	}

	public void SaveWorldPersistent(WorldSave new_WorldPersistent, bool cache, bool saveRocketsAndBranches, bool addToRevert, bool deleteRevert)
	{
		new_WorldPersistent = GetCopy(new_WorldPersistent);
		worldPersistent = Data<WorldSave>.Cache(new_WorldPersistent, cache);
		FolderPath path = Base.worldBase.paths.worldPersistentPath;
		bool isCareer = Base.worldBase.IsCareer;
		SaveAsync(delegate
		{
			if (deleteRevert)
			{
				Revert.DeleteAll();
			}
			WorldSave.Save(path, saveRocketsAndBranches, new_WorldPersistent, isCareer);
			if (addToRevert)
			{
				Revert.AddToStack(new_WorldPersistent, isCareer);
			}
		});
	}

	public void SaveRevertToLaunch(WorldSave new_RevertToLaunch, bool cache)
	{
		new_RevertToLaunch = GetCopy(new_RevertToLaunch);
		revertToLaunch = (cache ? new_RevertToLaunch : null);
		FolderPath path = Base.worldBase.paths.revertLaunchPath;
		bool isCareer = Base.worldBase.IsCareer;
		SaveAsync(delegate
		{
			WorldSave.Save(path, saveRocketsAndBranches: true, new_RevertToLaunch, isCareer);
		});
	}

	public static void SaveAsync(Action save)
	{
		if (save != null)
		{
			new Thread(save.Invoke).Start();
		}
	}

	public void UpdateWorldPersistent(Action<WorldSave> a)
	{
		if (worldPersistent != null && worldPersistent.result.data != null)
		{
			a(worldPersistent.result.data);
		}
	}

	public bool TryLoadBuildPersistent(I_MsgLogger logger, out Blueprint buildPersistent, bool eraseCache)
	{
		Preload_BlueprintPersistent();
		this.buildPersistent.JoinThread();
		if (this.buildPersistent.result.log != null)
		{
			logger.Log(this.buildPersistent.result.log);
		}
		buildPersistent = this.buildPersistent.result.data;
		bool item = this.buildPersistent.result.success;
		if (eraseCache)
		{
			this.buildPersistent = null;
		}
		return item;
	}

	public void Preload_BlueprintPersistent()
	{
		if (buildPersistent != null)
		{
			return;
		}
		FolderPath path = Base.worldBase.paths.buildPersistentPath;
		MsgCollector logger = new MsgCollector();
		buildPersistent = new Data<Blueprint>();
		buildPersistent.thread = new Thread((ThreadStart)delegate
		{
			if (path.FolderExists() && Blueprint.TryLoad(path, logger, out var blueprint))
			{
				buildPersistent.result = (success: true, data: blueprint, log: (logger.msg.Length > 0) ? logger.msg.ToString() : null);
			}
			else
			{
				buildPersistent.result = (success: false, data: null, log: null);
			}
		});
		buildPersistent.thread.Start();
	}

	public WorldSave LoadWorldPersistent(I_MsgLogger logger, bool needsRocketsAndBranches, bool eraseCache)
	{
		Preload_WorldPersistent(needsRocketsAndBranches);
		worldPersistent.JoinThread();
		if (worldPersistent.result.log != null)
		{
			logger.Log(worldPersistent.result.log);
		}
		WorldSave item = worldPersistent.result.data;
		if (eraseCache)
		{
			worldPersistent = null;
		}
		return GetCopy(item);
	}

	public void Preload_WorldPersistent(bool needsRocketsAndBranches)
	{
		if (worldPersistent != null && (!needsRocketsAndBranches || (worldPersistent.result.data.rockets != null && worldPersistent.result.data.branches != null)))
		{
			return;
		}
		WorldReference paths = Base.worldBase.paths;
		bool hasPersistent = paths.worldPersistentPath.FolderExists();
		MsgCollector logger = new MsgCollector();
		string version = Application.version;
		worldPersistent = new Data<WorldSave>();
		worldPersistent.thread = new Thread((ThreadStart)delegate
		{
			if (hasPersistent && WorldSave.TryLoad(paths.worldPersistentPath, needsRocketsAndBranches, logger, out var worldSave))
			{
				worldPersistent.result = (success: true, data: worldSave, log: (logger.msg.Length > 0) ? logger.msg.ToString() : null);
			}
			else
			{
				worldPersistent.result = (success: true, data: WorldSave.CreateEmptyQuicksave(version), log: null);
			}
		});
		worldPersistent.thread.Start();
	}

	public bool TryLoadRevertToLaunch(out WorldSave revertToLaunch)
	{
		if (this.revertToLaunch != null)
		{
			revertToLaunch = GetCopy(this.revertToLaunch);
			return true;
		}
		if (WorldSave.TryLoad(Base.worldBase.paths.revertLaunchPath, loadRocketsAndBranches: true, MsgDrawer.main, out revertToLaunch))
		{
			this.revertToLaunch = revertToLaunch;
			return true;
		}
		return false;
	}

	public void OnWorldExit()
	{
		buildPersistent = null;
		worldPersistent = null;
		revertToLaunch = null;
	}

	private static WorldSave GetCopy(WorldSave a)
	{
		return new WorldSave(a.version, GetCopy(a.career), GetCopy(a.astronauts), a.state, a.rockets, a.branches?.ToDictionary((KeyValuePair<int, Branch> e) => e.Key, delegate(KeyValuePair<int, Branch> e)
		{
			Branch value = e.Value;
			return new Branch
			{
				parentA = value.parentA,
				parentB = value.parentB,
				startTime = value.startTime,
				logEvents = value.logEvents.ToList(),
				challengeEvents = value.challengeEvents.ToList()
			};
		}), a.completeLogs.ToHashSet(), a.completeChallenges.ToHashSet());
	}

	public static WorldSave.CareerState GetCopy(WorldSave.CareerState a)
	{
		return new WorldSave.CareerState
		{
			funds = a.funds,
			unlocked_Parts = a.unlocked_Parts.ToList(),
			unlocked_Upgrades = a.unlocked_Upgrades.ToList()
		};
	}

	public static WorldSave.Astronauts GetCopy(WorldSave.Astronauts a)
	{
		return new WorldSave.Astronauts(a.astronauts.Select((WorldSave.Astronauts.Data e) => new WorldSave.Astronauts.Data(e.astronautName, e.alive)).ToList(), a.crew_World.Select((WorldSave.Astronauts.Crew_World e) => new WorldSave.Astronauts.Crew_World(e.astronautName)).ToList(), a.eva, a.flags, a.collectedRocks.ToDictionary((KeyValuePair<string, HashSet<long>> e) => e.Key, (KeyValuePair<string, HashSet<long>> e) => e.Value.ToHashSet()));
	}
}
