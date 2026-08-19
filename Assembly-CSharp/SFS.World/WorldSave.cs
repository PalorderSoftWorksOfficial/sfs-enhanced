using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFS.IO;
using SFS.Logs;
using SFS.Parsers.Json;
using SFS.Stats;
using SFS.Translations;
using SFS.Utilities;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.Scripting;

namespace SFS.World;

[Serializable]
public class WorldSave
{
	[Serializable]
	public class WorldState
	{
		public double worldTime;

		public int timewarpPhase;

		public bool mapView;

		public Double3 mapPosition;

		public string mapAddress;

		public string targetAddress;

		public string playerAddress;

		public float cameraDistance;

		public WorldState(double worldTime, int timewarpPhase, bool mapView, Double3 mapPosition, string mapAddress, string targetAddress, string playerAddress, float cameraDistance)
		{
			this.worldTime = worldTime;
			this.timewarpPhase = timewarpPhase;
			this.mapView = mapView;
			this.mapPosition = mapPosition;
			this.mapAddress = mapAddress;
			this.targetAddress = targetAddress;
			this.playerAddress = playerAddress;
			this.cameraDistance = cameraDistance;
		}

		public static WorldState StartState()
		{
			bool flag = Base.worldBase != null && Base.worldBase.insideWorld.Value;
			return new WorldState(1000000.0, 0, mapView: false, flag ? new Double3(Base.planetLoader.spaceCenter.LaunchPadLocation.position.x, Base.planetLoader.spaceCenter.LaunchPadLocation.position.y, 30000.0) : default(Double3), flag ? WorldAddress.GetMapAddress(Base.planetLoader.spaceCenter.Planet.mapPlanet) : null, "null", "null", 35f);
		}
	}

	[Serializable]
	public class Astronauts
	{
		[Serializable]
		public class Data
		{
			public string astronautName;

			public bool alive;

			public Data()
			{
			}

			public Data(string astronautName, bool alive)
			{
				this.astronautName = astronautName;
				this.alive = alive;
			}
		}

		[Serializable]
		public class Crew_World
		{
			public string astronautName;

			public Crew_World()
			{
			}

			public Crew_World(string astronautName)
			{
				this.astronautName = astronautName;
			}
		}

		[Serializable]
		[JsonConverter(typeof(LocationData.LocationConverter))]
		public class EVA
		{
			public string astronautName;

			public LocationData location;

			public float rotation;

			public float angularVelocity;

			public double fuelPercent;

			public float temperature;

			public bool ragdoll;

			public int branch = -1;

			public EVA()
			{
			}

			public EVA(Astronaut_EVA a)
			{
				astronautName = a.astronaut.astronautName;
				location = new LocationData(a.location.Value);
				rotation = a.rb2d.rotation;
				angularVelocity = a.rb2d.angularVelocity;
				fuelPercent = a.resources.fuelPercent.Value;
				temperature = a.resources.temperature.Value;
				ragdoll = a.ragdoll;
				branch = a.stats.branch;
			}
		}

		[Serializable]
		[JsonConverter(typeof(LocationData.LocationConverter))]
		public class Flag
		{
			public LocationData location;

			public int direction;

			public Flag()
			{
			}

			public Flag(SFS.World.Flag flag)
			{
				location = new LocationData(flag.location.Value);
				direction = flag.direction;
			}
		}

		public List<Data> astronauts = new List<Data>();

		public List<Crew_World> crew_World = new List<Crew_World>();

		public List<EVA> eva = new List<EVA>();

		public List<Flag> flags = new List<Flag>();

		public Dictionary<string, HashSet<long>> collectedRocks = new Dictionary<string, HashSet<long>>();

		public Astronauts()
		{
		}

		public Astronauts(List<Data> astronauts, List<Crew_World> crew_World, List<EVA> eva, List<Flag> flags, Dictionary<string, HashSet<long>> collectedRocks)
		{
			this.astronauts = astronauts;
			this.crew_World = crew_World;
			this.eva = eva;
			this.flags = flags;
			this.collectedRocks = collectedRocks;
		}
	}

	[Serializable]
	public class CareerState
	{
		public enum UnlockType
		{
			Part,
			Upgrade
		}

		public double funds;

		public List<string> unlocked_Parts = new List<string>();

		public List<string> unlocked_Upgrades = new List<string>();

		public static (UnlockType, string) heatShieldFeature = (UnlockType.Part, "Heat Shield_0_0");

		public static (UnlockType, string) throttleFeature = (UnlockType.Upgrade, "Liquids 1");

		public static (UnlockType, string) completedTree_1 = (UnlockType.Upgrade, "Landing");

		public static (UnlockType, string) completedTree_2 = (UnlockType.Part, "Engine Valiant_0_0");
	}

	[Serializable]
	public class LocationData
	{
		[Preserve]
		public class LocationConverter : JsonConverter
		{
			private const string LocationVariableName = "location";

			public override bool CanConvert(Type objectType)
			{
				return true;
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				JObject jObject = JObject.Load(reader);
				object obj = Activator.CreateInstance(objectType);
				FieldInfo[] fields = objectType.GetFields();
				foreach (FieldInfo fieldInfo in fields)
				{
					fieldInfo.SetValue(obj, jObject[fieldInfo.Name]?.ToObject(fieldInfo.FieldType) ?? fieldInfo.GetValue(obj));
				}
				objectType.GetField("location").SetValue(obj, (jObject["location"] != null) ? ReadLocationData(jObject["location"].Value<JObject>()) : ReadLocationData(jObject));
				return obj;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				JObject jObject = new JObject();
				FieldInfo[] fields = value.GetType().GetFields();
				foreach (FieldInfo fieldInfo in fields)
				{
					object value2 = fieldInfo.GetValue(value);
					if (value2 != null)
					{
						jObject.Add(fieldInfo.Name, JToken.FromObject(value2, serializer));
					}
				}
				jObject.WriteTo(writer);
			}

			private LocationData ReadLocationData(JObject jObject)
			{
				return new LocationData
				{
					address = (jObject["address"] ?? jObject["adress"]).ToObject<string>(),
					position = (jObject["position"]?.ToObject<Double2>() ?? Double2.zero),
					velocity = (jObject["velocity"]?.ToObject<Double2>() ?? Double2.zero)
				};
			}
		}

		public string address;

		public Double2 position;

		public Double2 velocity;

		public LocationData()
		{
		}

		public LocationData(Location location)
		{
			address = location.planet.codeName;
			position = location.position;
			velocity = location.velocity;
		}

		public Location GetSaveLocation(double time)
		{
			return new Location(time, address.GetPlanet(), position, velocity);
		}
	}

	public string version;

	public CareerState career;

	public WorldState state;

	public Astronauts astronauts;

	public RocketSave[] rockets;

	public Dictionary<int, Branch> branches;

	public HashSet<LogId> completeLogs;

	public HashSet<string> completeChallenges;

	public static void Save(FolderPath path, bool saveRocketsAndBranches, WorldSave worldSave, bool isCareer)
	{
		if (!path.FolderExists())
		{
			path.CreateFolder();
		}
		JsonWrapper.SaveAsJson(path.ExtendToFile("Version.txt"), worldSave.version, pretty: false);
		JsonWrapper.SaveAsJson(path.ExtendToFile("WorldState.txt"), worldSave.state, pretty: false);
		if (saveRocketsAndBranches)
		{
			JsonWrapper.SaveAsJson(path.ExtendToFile("Rockets.txt"), worldSave.rockets, pretty: false);
			JsonWrapper.SaveAsJson(path.ExtendToFile("Branches.txt"), worldSave.branches, pretty: false);
		}
		else if (!path.ExtendToFile("Rockets.txt").FileExists())
		{
			JsonWrapper.SaveAsJson(path.ExtendToFile("Rockets.txt"), new RocketSave[0], pretty: false);
		}
		JsonWrapper.SaveAsJson(path.ExtendToFile("Achievements.txt"), worldSave.completeLogs, pretty: false);
		JsonWrapper.SaveAsJson(path.ExtendToFile("Challenges.txt"), worldSave.completeChallenges, pretty: false);
		if (isCareer)
		{
			Save_CareerState(path, worldSave.career);
		}
		if (!DevSettings.DisableAstronauts)
		{
			Save_AstronautStates(path, worldSave.astronauts);
		}
	}

	public static bool TryLoad(FolderPath path, bool loadRocketsAndBranches, I_MsgLogger logger, out WorldSave worldSave)
	{
		RocketSave[] data = null;
		if (loadRocketsAndBranches && !JsonWrapper.TryLoadJson<RocketSave[]>(path.ExtendToFile("Rockets.txt"), out data))
		{
			logger.Log(Loc.main.Load_Failed.InjectField(Loc.main.Quicksave, "filetype").Inject(path, "filepath"));
			worldSave = null;
			return false;
		}
		string text = (JsonWrapper.TryLoadJson<string>(path.ExtendToFile("Version.txt"), out var data2) ? data2 : Application.version);
		WorldState worldState = (JsonWrapper.TryLoadJson<WorldState>(path.ExtendToFile("WorldState.txt"), out var data3) ? data3 : WorldState.StartState());
		Dictionary<int, Branch> dictionary = ((!loadRocketsAndBranches) ? null : (JsonWrapper.TryLoadJson<Dictionary<int, Branch>>(path.ExtendToFile("Branches.txt"), out var data4) ? data4 : new Dictionary<int, Branch>()));
		CareerState careerState = (JsonWrapper.TryLoadJson<CareerState>(path.ExtendToFile("CareerState.txt"), out var data5) ? data5 : new CareerState());
		Astronauts astronauts = (JsonWrapper.TryLoadJson<Astronauts>(path.ExtendToFile("Astronauts.txt"), out var data6) ? data6 : new Astronauts());
		HashSet<LogId> hashSet = (JsonWrapper.TryLoadJson<List<LogId>>(path.ExtendToFile("Achievements.txt"), out var data7) ? data7.ToHashSet() : new HashSet<LogId>());
		HashSet<string> hashSet2 = (JsonWrapper.TryLoadJson<List<string>>(path.ExtendToFile("Challenges.txt"), out var data8) ? data8.ToHashSet() : new HashSet<string>());
		worldSave = new WorldSave(text, careerState, astronauts, worldState, data, dictionary, hashSet, hashSet2);
		return true;
	}

	public static void Save_CareerState(FolderPath path, CareerState careerState)
	{
		JsonWrapper.SaveAsJson(path.ExtendToFile("CareerState.txt"), careerState, pretty: false);
		SavingCache.main.UpdateWorldPersistent(delegate(WorldSave a)
		{
			a.career = SavingCache.GetCopy(careerState);
		});
	}

	public static void Save_AstronautStates(FolderPath path, Astronauts astronautStates)
	{
		JsonWrapper.SaveAsJson(path.ExtendToFile("Astronauts.txt"), astronautStates, pretty: false);
		SavingCache.main.UpdateWorldPersistent(delegate(WorldSave a)
		{
			a.astronauts = SavingCache.GetCopy(astronautStates);
		});
	}

	public static WorldState Load_WorldState(FolderPath path)
	{
		if (!JsonWrapper.TryLoadJson<WorldState>(path.ExtendToFile("WorldState.txt"), out var data) || data == null)
		{
			return WorldState.StartState();
		}
		return data;
	}

	public static WorldSave CreateEmptyQuicksave(string version)
	{
		return new WorldSave(version, new CareerState(), new Astronauts(), WorldState.StartState(), new RocketSave[0], new Dictionary<int, Branch>(), new HashSet<LogId>(), new HashSet<string>());
	}

	public WorldSave(string version, CareerState career, Astronauts astronauts, WorldState state, RocketSave[] rockets, Dictionary<int, Branch> branches, HashSet<LogId> completeLogs, HashSet<string> completeChallenges)
	{
		this.version = version;
		this.career = career;
		this.astronauts = astronauts;
		this.state = state;
		this.rockets = rockets;
		this.branches = branches;
		this.completeLogs = completeLogs;
		this.completeChallenges = completeChallenges;
	}
}
