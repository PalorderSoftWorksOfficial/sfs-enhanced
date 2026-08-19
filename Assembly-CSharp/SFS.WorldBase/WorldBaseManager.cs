using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Core;
using SFS.Logs;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.WorldBase;

public class WorldBaseManager : MonoBehaviour
{
	public TimeEvent timeManager;

	public Bool_Local insideWorld = new Bool_Local();

	[NonSerialized]
	public WorldReference paths;

	[NonSerialized]
	public WorldSettings settings;

	public Dictionary<string, Challenge> challenges;

	public Challenge[] challengesArray;

	public GameObject fuelManager;

	public bool AllowsCheats => settings.mode.AllowsCheats;

	public bool IsCareer => settings.mode.mode == WorldMode.Mode.Career;

	public bool IsChallenge => settings.mode.mode == WorldMode.Mode.Challenge;

	public static void EnterWorld(string worldName, Action callback)
	{
		WorldReference worldReference = new WorldReference(worldName);
		WorldSettings worldSettings = worldReference.LoadWorldSettings();
		if (!DevSettings.FullVersion)
		{
			if (worldSettings.solarSystem.name != "")
			{
				Menu.read.Open(() => "Full version is needed to load custom solar systems");
				return;
			}
			worldSettings.cheats = new SandboxSettings.Data();
		}
		if (!worldSettings.mode.AllowsCheats)
		{
			worldSettings.cheats = new SandboxSettings.Data();
		}
		Base.worldBase.EnterWorld(worldReference, worldSettings, callback);
	}

	private void EnterWorld(WorldReference paths, WorldSettings settings, Action callback)
	{
		this.settings = settings;
		Menu.loading.Open();
		MsgCollector log = new MsgCollector();
		Base.planetLoader.LoadSolarSystem(settings, log, delegate(bool success)
		{
			Menu.loading.Close();
			if (log.msg.Length > 0)
			{
				Menu.read.Open(() => log.msg.ToString(), delegate
				{
					ActionQueue.main.QueueAction(Callback);
				});
			}
			else
			{
				Callback();
			}
			void Callback()
			{
				if (success)
				{
					insideWorld.Value = true;
					this.paths = paths;
					this.settings = settings;
					SandboxSettings.main.settings = settings.cheats;
					SandboxSettings.main.UpdateUI(instantAnimation: true);
					TimeEvent timeEvent = timeManager;
					timeEvent.on_10000Ms = (Action)Delegate.Combine(timeEvent.on_10000Ms, new Action(UpdateWorldPlaytime));
					challengesArray = Challenge.CollectChallenges().ToArray();
					challenges = challengesArray.ToDictionary((Challenge m) => m.id);
					callback();
				}
				else
				{
					this.settings = null;
				}
			}
		});
	}

	public void ExitWorld()
	{
		insideWorld.Value = false;
		paths = null;
		settings = null;
		SandboxSettings.main.settings = new SandboxSettings.Data();
		SandboxSettings.main.UpdateUI(instantAnimation: true);
		TimeEvent timeEvent = timeManager;
		timeEvent.on_10000Ms = (Action)Delegate.Remove(timeEvent.on_10000Ms, new Action(UpdateWorldPlaytime));
		challengesArray = null;
		challenges = null;
		SavingCache.main.OnWorldExit();
	}

	private void UpdateWorldPlaytime()
	{
		settings.playtime.lastPlayedTime_Ticks = DateTime.Now.Ticks;
		settings.playtime.totalPlayTime_Seconds += 10.0;
		paths.SaveWorldSettings(settings);
	}

	private void Start()
	{
	}
}
