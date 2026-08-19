using System;
using System.Linq;
using SFS.World;
using SFS.World.Maps;
using SFS.WorldBase;

namespace SFS.Utilities;

public static class WorldAddress
{
	public const string Null = "null";

	public static string GetMapAddress(SelectableObject mapInstance)
	{
		if (!(mapInstance is MapPlanet mapPlanet))
		{
			if (!(mapInstance is MapRocket mapRocket))
			{
				if (!(mapInstance is MapAstronaut mapAstronaut))
				{
					if (mapInstance is MapFlag mapFlag)
					{
						return GetFlagAddress(AstronautManager.main.flags.IndexOf(mapFlag.flag));
					}
					return "null";
				}
				return GetAstronautAddress(mapAstronaut.astronaut.astronaut.astronautName);
			}
			return GetRocketAddress(GameManager.main.rockets.IndexOf(mapRocket.rocket));
		}
		return GetPlanetAddress(mapPlanet.planet);
	}

	public static SelectableObject GetMapInstance(string address, bool allowNull = false)
	{
		if (address == null || address == "null")
		{
			return NullCase();
		}
		if (address.StartsWith("Planet:") && address.Split(':', StringSplitOptions.None)[1].HasPlanet())
		{
			return address.Split(':', StringSplitOptions.None)[1].GetPlanet().mapPlanet;
		}
		if (address.HasPlanet())
		{
			return address.GetPlanet().mapPlanet;
		}
		if (TryGetRocketIndex_SupportsLegacy(address, out var rocketIndex) && GameManager.main.rockets.IsValidIndex(rocketIndex))
		{
			return GameManager.main.rockets[rocketIndex].mapPlayer;
		}
		if (address.StartsWith("Astronaut:") && AstronautManager.main.eva.Any((Astronaut_EVA a) => a.astronaut.astronautName == address.Split(':', StringSplitOptions.None)[1]))
		{
			return AstronautManager.main.eva.First((Astronaut_EVA a) => a.astronaut.astronautName == address.Split(':', StringSplitOptions.None)[1]).mapPlayer;
		}
		if (address.StartsWith("Flag:") && int.TryParse(address.Split(':', StringSplitOptions.None)[1], out var result) && AstronautManager.main.flags.IsValidIndex(result))
		{
			return AstronautManager.main.flags[result].mapPlayer;
		}
		return NullCase();
		SelectableObject NullCase()
		{
			if (!allowNull)
			{
				return Base.planetLoader.spaceCenter.Planet.mapPlanet;
			}
			return null;
		}
	}

	public static string GetPlayerAddress(Player player)
	{
		if (!(player is Rocket item))
		{
			if (!(player is Astronaut_EVA astronaut_EVA))
			{
				if (player is Flag item2)
				{
					return GetFlagAddress(AstronautManager.main.flags.IndexOf(item2));
				}
				return "null";
			}
			return GetAstronautAddress(astronaut_EVA.astronaut.astronautName);
		}
		return GetRocketAddress(GameManager.main.rockets.IndexOf(item));
	}

	public static Player GetPlayerInstance(string address)
	{
		if (address == null || address == "null")
		{
			return null;
		}
		if (TryGetRocketIndex_SupportsLegacy(address, out var rocketIndex) && GameManager.main.rockets.IsValidIndex(rocketIndex))
		{
			return GameManager.main.rockets[rocketIndex];
		}
		if (address.StartsWith("Astronaut:") && AstronautManager.main.eva.Any((Astronaut_EVA astronaut) => astronaut.astronaut.astronautName == address.Split(':', StringSplitOptions.None)[1]))
		{
			return AstronautManager.main.eva.First((Astronaut_EVA astronaut) => astronaut.astronaut.astronautName == address.Split(':', StringSplitOptions.None)[1]);
		}
		if (address.StartsWith("Flag:") && int.TryParse(address.Split(':', StringSplitOptions.None)[1], out var result) && AstronautManager.main.flags.IsValidIndex(result))
		{
			return AstronautManager.main.flags[result];
		}
		return null;
	}

	public static Location GetPlayerLocationFromSave(WorldSave save)
	{
		string address = save.state.playerAddress;
		if (address == null || address == "null")
		{
			return Base.planetLoader.spaceCenter.LaunchPadLocation;
		}
		if (TryGetRocketIndex_SupportsLegacy(address, out var rocketIndex) && save.rockets.IsValidIndex(rocketIndex))
		{
			return save.rockets[rocketIndex].location.GetSaveLocation(save.state.worldTime);
		}
		if (address.StartsWith("Astronaut:") && save.astronauts.eva.Any((WorldSave.Astronauts.EVA a) => a.astronautName == address.Split(':', StringSplitOptions.None)[1]))
		{
			return save.astronauts.eva.First((WorldSave.Astronauts.EVA a) => a.astronautName == address.Split(':', StringSplitOptions.None)[1]).location.GetSaveLocation(save.state.worldTime);
		}
		if (address.StartsWith("Flag:") && int.TryParse(address.Split(':', StringSplitOptions.None)[1], out var result) && save.astronauts.flags.IsValidIndex(result))
		{
			return save.astronauts.flags[result].location.GetSaveLocation(save.state.worldTime);
		}
		return Base.planetLoader.spaceCenter.LaunchPadLocation;
	}

	private static bool TryGetRocketIndex_SupportsLegacy(string address, out int rocketIndex)
	{
		if (address.StartsWith("Rocket:") && int.TryParse(address.Split(':', StringSplitOptions.None)[1], out rocketIndex))
		{
			return true;
		}
		if (int.TryParse(address, out var result))
		{
			rocketIndex = result;
			return true;
		}
		rocketIndex = -1;
		return false;
	}

	private static string GetPlanetAddress(Planet planet)
	{
		return "Planet:" + planet.codeName;
	}

	private static string GetRocketAddress(int rocketIndex)
	{
		if (rocketIndex != -1)
		{
			return "Rocket:" + rocketIndex;
		}
		return "null";
	}

	private static string GetAstronautAddress(string astronautName)
	{
		return "Astronaut:" + astronautName;
	}

	private static string GetFlagAddress(int flagIndex)
	{
		return "Flag: " + flagIndex;
	}

	public static bool TryGetPlanetFromAddress(string address, out string planetName)
	{
		if (address.StartsWith("Planet:") && address.Split(':', StringSplitOptions.None)[1].HasPlanet())
		{
			planetName = address.Split(':', StringSplitOptions.None)[1];
			return true;
		}
		if (address.HasPlanet())
		{
			planetName = address;
			return true;
		}
		planetName = null;
		return false;
	}
}
