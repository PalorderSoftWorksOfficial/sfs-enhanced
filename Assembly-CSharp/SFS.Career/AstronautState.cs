using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SFS.Builds;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.Career;

public class AstronautState : MonoBehaviour
{
	public enum State
	{
		Available,
		CrewBuild,
		EVA,
		CrewWorld,
		Deceased
	}

	public static AstronautState main;

	public List<string> crew_Build;

	public WorldSave.Astronauts state;

	public bool selfManageSaving;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		if (selfManageSaving)
		{
			state = SavingCache.main.LoadWorldPersistent(MsgDrawer.main, needsRocketsAndBranches: false, eraseCache: false).astronauts;
		}
	}

	private void Save()
	{
		WorldSave.Save_AstronautStates(Base.worldBase.paths.worldPersistentPath, state);
	}

	public void CreateAstronaut(string astronautName)
	{
		astronautName = Regex.Replace(astronautName, "[^A-Za-z0-9 ]", "");
		if (astronautName == "")
		{
			Menu.read.Open(() => "Invalid astronaut name");
			return;
		}
		if (HasAstronaut(astronautName))
		{
			Menu.read.Open(() => "Astronaut already exists");
			return;
		}
		state.astronauts.Add(new WorldSave.Astronauts.Data(astronautName, alive: true));
		if (selfManageSaving)
		{
			Save();
		}
	}

	public void FireAstronaut(string astronautName)
	{
		state.astronauts.RemoveAll((WorldSave.Astronauts.Data a) => a.astronautName == astronautName);
		if (selfManageSaving)
		{
			Save();
		}
	}

	public void AddCrew(string astronautName)
	{
		if (BuildManager.main != null)
		{
			crew_Build.Add(astronautName);
		}
		else
		{
			state.crew_World.Add(new WorldSave.Astronauts.Crew_World(astronautName));
		}
	}

	public void RemoveCrew(string astronautName)
	{
		if (BuildManager.main != null)
		{
			crew_Build.Remove(astronautName);
			return;
		}
		state.crew_World.RemoveAll((WorldSave.Astronauts.Crew_World a) => a.astronautName == astronautName);
	}

	private bool HasAstronaut(string name)
	{
		return GetAstronautByName(name) != null;
	}

	public WorldSave.Astronauts.Data GetAstronautByName(string astronautName)
	{
		return state.astronauts.FirstOrDefault((WorldSave.Astronauts.Data a) => a.astronautName == astronautName);
	}

	public WorldSave.Astronauts GetAstronautSave_Game()
	{
		return new WorldSave.Astronauts
		{
			astronauts = state.astronauts,
			crew_World = state.crew_World,
			eva = (from a in AstronautManager.main.eva
				where a.astronaut.alive
				select new WorldSave.Astronauts.EVA(a)).ToList(),
			flags = AstronautManager.main.flags.Select((Flag f) => new WorldSave.Astronauts.Flag(f)).ToList(),
			collectedRocks = state.collectedRocks
		};
	}

	public string GetAstronautStateText(State state, bool forSelect)
	{
		return state switch
		{
			State.Available => forSelect ? Loc.main.Crew_Available : Loc.main.Crew_AwaitingMission, 
			State.CrewBuild => forSelect ? Loc.main.Crew_Assigned : Loc.main.Crew_AwaitingMission, 
			State.CrewWorld => Loc.main.Crew_In_Flight, 
			State.EVA => Loc.main.Crew_In_EVA, 
			State.Deceased => Loc.main.Crew_Deceased, 
			_ => throw new Exception(), 
		};
	}

	public State GetAstronautState(string astronautName)
	{
		if (BuildManager.main != null && crew_Build.Any((string crew) => crew == astronautName))
		{
			return State.CrewBuild;
		}
		if (state.crew_World.Any((WorldSave.Astronauts.Crew_World crew) => crew.astronautName == astronautName))
		{
			return State.CrewWorld;
		}
		if ((GameManager.main != null) ? AstronautManager.main.eva.Any((Astronaut_EVA a) => a.astronaut.astronautName == astronautName) : state.eva.Any((WorldSave.Astronauts.EVA a) => a.astronautName == astronautName))
		{
			return State.EVA;
		}
		if (state.astronauts.Any((WorldSave.Astronauts.Data a) => a.alive && a.astronautName == astronautName))
		{
			return State.Available;
		}
		return State.Deceased;
	}
}
