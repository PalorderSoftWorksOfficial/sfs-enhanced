using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using SFS.IO;
using SFS.Parsers.Json;
using SFS.World;
using SFS.World.Legacy;
using SFS.World.PlanetModules;
using UnityEngine;

namespace SFS.WorldBase;

public class PlanetLoader : MonoBehaviour
{
	private const string Version_File = "Version.txt";

	private const string Planet_Data_Version_File = "Version.txt";

	private const string ImportSetting_File = "Import_Settings.txt";

	private const string SpaceCenter_File = "Space_Center_Data.txt";

	private const string Planets_Folder = "Planet Data";

	private const string Heightmap_Folder = "Heightmap Data";

	private const string Textures_Folder = "Texture Data";

	public Shader terrainShader;

	public Shader atmosphereShader;

	public GameObject planetHolder;

	public SolarSystemSettings solarSystemSettings;

	public SpaceCenterData spaceCenter;

	public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

	public Dictionary<string, HeightMap> heightmaps = new Dictionary<string, HeightMap>();

	public Dictionary<string, Planet> planets = new Dictionary<string, Planet>();

	private void Start()
	{
		if (!Application.isEditor && DevSettings.FullVersion)
		{
			ExportExampleAsync();
		}
	}

	private static void ExportExampleAsync()
	{
		FolderPath exampleFolder = FileLocations.SolarSystemsFolder.Extend("Example");
		FilePath planetDataVersionFile = exampleFolder.ExtendToFile("Version.txt");
		if (exampleFolder.FolderExists() && planetDataVersionFile.FileExists() && planetDataVersionFile.ReadText() == "Planet_Data_v1")
		{
			return;
		}
		string version = Application.version;
		Dictionary<string, HeightMap> heightMaps = new Dictionary<string, HeightMap>();
		Dictionary<string, PlanetData> planetData = new Dictionary<string, PlanetData>();
		LoadHeightmaps_Private(heightMaps);
		LoadPlanets_Private(planetData);
		new Thread((ThreadStart)delegate
		{
			exampleFolder.DeleteFolder();
			exampleFolder.CreateFolder();
			planetDataVersionFile.WriteText("Planet_Data_v1");
			exampleFolder.ExtendToFile("Version.txt").WriteText(version);
			exampleFolder.ExtendToFile("Import_Settings.txt").WriteText(JsonWrapper.ToJson(new SolarSystemSettings(), pretty: true));
			exampleFolder.ExtendToFile("Space_Center_Data.txt").WriteText(JsonWrapper.ToJson(new SpaceCenterData(), pretty: true));
			FolderPath folderPath = exampleFolder.CloneAndExtend("Planet Data");
			folderPath.CreateFolder();
			foreach (KeyValuePair<string, PlanetData> item in planetData)
			{
				folderPath.ExtendToFile(item.Key + ".txt").WriteText(JsonWrapper.ToJson(item.Value, pretty: true));
			}
			FolderPath folderPath2 = exampleFolder.CloneAndExtend("Heightmap Data");
			folderPath2.CreateFolder();
			foreach (KeyValuePair<string, HeightMap> item2 in heightMaps)
			{
				folderPath2.ExtendToFile(item2.Key + ".txt").WriteText(JsonUtility.ToJson(item2.Value, prettyPrint: true));
			}
			exampleFolder.CloneAndExtend("Texture Data").CreateFolder();
		}).Start();
	}

	public void LoadSolarSystem(WorldSettings settings, I_MsgLogger log, Action<bool> callback)
	{
		UnloadSolarSystem();
		SolarSystemReference solarSystem = settings.solarSystem;
		if (solarSystem.name.Length > 0)
		{
			FolderPath folderPath = FileLocations.SolarSystemsFolder.Extend(solarSystem.name);
			if (!folderPath.FolderExists())
			{
				log.Log("Solar system " + solarSystem.name + " does not exist");
				callback(obj: false);
				return;
			}
			FilePath filePath = folderPath.ExtendToFile("Import_Settings.txt");
			if (!filePath.FileExists())
			{
				log.Log("Solar system " + solarSystem.name + " does not have Import_Settings.txt file");
				callback(obj: false);
				return;
			}
			if (!JsonWrapper.TryLoadJson<SolarSystemSettings>(filePath, out solarSystemSettings))
			{
				log.Log("Failed to load import settings file");
				callback(obj: false);
				return;
			}
			Dictionary<string, PlanetData> dictionary = new Dictionary<string, PlanetData>();
			if (solarSystemSettings.includeDefaultPlanets)
			{
				LoadPlanets_Private(dictionary);
			}
			if (solarSystemSettings.includeDefaultTextures)
			{
				LoadTextures_Private(textures);
			}
			if (solarSystemSettings.includeDefaultHeightmaps)
			{
				LoadHeightmaps_Private(heightmaps);
			}
			LoadPlanets_Public(solarSystem, dictionary, log);
			LoadTextures_Public(solarSystem, textures, log);
			LoadHeightmaps_Public(solarSystem, heightmaps, log);
			dictionary.ForEach(delegate(KeyValuePair<string, PlanetData> planet)
			{
				settings.difficulty.ScalePlanetData(planet.Value);
			});
			planets = CreatePlanets(dictionary, log, out var success);
			LoadSpaceCenter_Public(solarSystem, log);
			callback(success);
		}
		else
		{
			solarSystemSettings = new SolarSystemSettings();
			spaceCenter = new SpaceCenterData();
			Dictionary<string, PlanetData> dictionary2 = new Dictionary<string, PlanetData>();
			LoadPlanets_Private(dictionary2);
			LoadTextures_Private(textures);
			LoadHeightmaps_Private(heightmaps);
			dictionary2.ForEach(delegate(KeyValuePair<string, PlanetData> planet)
			{
				settings.difficulty.ScalePlanetData(planet.Value);
			});
			planets = CreatePlanets(dictionary2, log, out var _);
			callback(obj: true);
		}
	}

	private void UnloadSolarSystem()
	{
		foreach (Planet value in planets.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		textures.Clear();
		heightmaps.Clear();
		planets.Clear();
	}

	private static void LoadTextures_Private(Dictionary<string, Texture2D> output)
	{
		Texture2D[] array = UnityEngine.Resources.LoadAll<Texture2D>("Planet_Textures");
		foreach (Texture2D texture2D in array)
		{
			output[texture2D.name] = texture2D;
		}
	}

	private static void LoadHeightmaps_Private(Dictionary<string, HeightMap> output)
	{
		TextAsset[] array = UnityEngine.Resources.LoadAll<TextAsset>("Planet_Heightmaps");
		foreach (TextAsset textAsset in array)
		{
			if (TryLoadHeightmap(textAsset.text, out var heightmap))
			{
				output[textAsset.name] = heightmap;
			}
		}
	}

	private static void LoadPlanets_Private(Dictionary<string, PlanetData> output)
	{
		List<string> list = new List<string> { "Sun", "Earth", "Moon", "Near_Earth_Asteroid" };
		TextAsset[] array = UnityEngine.Resources.LoadAll<TextAsset>("Planet_Data");
		foreach (TextAsset textAsset in array)
		{
			if (DevSettings.FullVersion || list.Contains(textAsset.name))
			{
				output[textAsset.name] = JsonWrapper.FromJson<PlanetData>(textAsset.text);
			}
		}
	}

	private void LoadSpaceCenter_Public(SolarSystemReference solarSystem, I_MsgLogger log)
	{
		FilePath filePath = FileLocations.SolarSystemsFolder.Extend(solarSystem.name).ExtendToFile("Space_Center_Data.txt");
		if (!filePath.FileExists() || !JsonWrapper.TryLoadJson<SpaceCenterData>(filePath, out spaceCenter))
		{
			FilePath filePath2 = FileLocations.SolarSystemsFolder.Extend(solarSystem.name).ExtendToFile("Launch_Pad_Position.txt");
			if (filePath2.FileExists() && JsonWrapper.TryLoadJson<LegacyLaunchPad>(filePath2, out var data))
			{
				spaceCenter = SpaceCenterData.FromLegacyLaunchLocation(data);
			}
			else
			{
				log.Log("Solar system " + solarSystem.name + " does not have Space_Center_Data.txt file");
			}
		}
	}

	private static void LoadTextures_Public(SolarSystemReference solarSystem, Dictionary<string, Texture2D> outputTextures, I_MsgLogger log)
	{
		FolderPath folderPath = FileLocations.SolarSystemsFolder.Extend(solarSystem.name).Extend("Texture Data");
		if (!folderPath.FolderExists())
		{
			log.Log("Solar system " + solarSystem.name + " does not have Texture Data folder");
			return;
		}
		foreach (FilePath item in folderPath.GetFilesInFolder(recursively: false))
		{
			if (Application.isEditor && item.Extension.ToLowerInvariant() == "meta")
			{
				continue;
			}
			if (item.Extension.ToLowerInvariant() == "png" || item.Extension.ToLowerInvariant() == "jpg" || item.Extension.ToLowerInvariant() == "jpeg")
			{
				Texture2D texture2D = new Texture2D(0, 0);
				if (texture2D.LoadImage(item.ReadBytes()))
				{
					outputTextures[item.CleanFileName] = texture2D;
				}
				else
				{
					log.Log("ERROR: loading texture failed: " + item.CleanFileName);
				}
			}
			else
			{
				log.Log("ERROR: texture format invalid: " + item.Extension);
			}
		}
	}

	private static void LoadHeightmaps_Public(SolarSystemReference solarSystem, Dictionary<string, HeightMap> outputHeightmaps, I_MsgLogger log)
	{
		FolderPath folderPath = FileLocations.SolarSystemsFolder.Extend(solarSystem.name).Extend("Heightmap Data");
		if (!folderPath.FolderExists())
		{
			log.Log("Solar system " + solarSystem.name + " does not have Heightmap Data folder");
			return;
		}
		foreach (FilePath item in folderPath.GetFilesInFolder(recursively: false))
		{
			if (Application.isEditor && item.Extension.ToLowerInvariant() == "meta")
			{
				continue;
			}
			if (item.Extension.ToLowerInvariant() == "png" || item.Extension.ToLowerInvariant() == "jpg" || item.Extension.ToLowerInvariant() == "jpeg")
			{
				Texture2D texture2D = new Texture2D(0, 0);
				if (texture2D.LoadImage(item.ReadBytes()))
				{
					outputHeightmaps[item.FileName] = new HeightMap(texture2D);
				}
				else
				{
					log.Log("ERROR: loading heightmap failed: " + item.CleanFileName);
				}
				UnityEngine.Object.Destroy(texture2D);
			}
			else if (item.Extension.ToLowerInvariant() == "txt")
			{
				if (TryLoadHeightmap(item.ReadText(), out var heightmap))
				{
					outputHeightmaps[item.FileName] = heightmap;
				}
				else
				{
					log.Log("ERROR: loading heightmap failed: " + item.CleanFileName);
				}
			}
			else
			{
				log.Log("ERROR: heightmap format invalid: " + item.Extension);
			}
		}
	}

	private static void LoadPlanets_Public(SolarSystemReference solarSystem, Dictionary<string, PlanetData> outputPlanetData, I_MsgLogger log)
	{
		FolderPath folderPath = FileLocations.SolarSystemsFolder.Extend(solarSystem.name).Extend("Planet Data");
		if (!folderPath.FolderExists())
		{
			log.Log("Solar system does not have Planet Data folder");
			return;
		}
		FilePath[] array = folderPath.GetFilesInFolder(recursively: false).ToArray();
		int num = 0;
		FilePath[] array2 = array;
		foreach (FilePath filePath in array2)
		{
			if (Application.isEditor && filePath.Extension.ToLowerInvariant() == "meta")
			{
				continue;
			}
			if (filePath.Extension.ToLowerInvariant() != "txt")
			{
				log.Log("ERROR: planet format invalid: " + filePath.Extension);
				continue;
			}
			PlanetData value = LegacyConverter.CheckAndConvert_Planet(filePath.CleanFileName, filePath.ReadText(), log, out var converted, out var success);
			if (!success)
			{
				log.Log("ERROR: Loading planet failed: " + filePath.CleanFileName);
				continue;
			}
			if (converted)
			{
				num++;
			}
			if (outputPlanetData.ContainsKey(filePath.CleanFileName))
			{
				log.Log("ERROR: Already has a planet named: " + filePath.CleanFileName);
			}
			else
			{
				outputPlanetData[filePath.CleanFileName] = value;
			}
		}
		if (num > 0 && FileLocations.GetOneTimeNotification("Converted_Planets"))
		{
			log.Log("Found " + num + " legacy planet files and converted them automatically");
		}
	}

	private static bool TryLoadHeightmap(string json, out HeightMap heightmap)
	{
		List<float> list = new List<float>();
		int num = -1;
		for (int i = 0; i < json.Length; i++)
		{
			char c = json[i];
			if (char.IsNumber(c) || c == '.' || c == '-' || c == 'e')
			{
				if (num == -1)
				{
					num = i;
				}
			}
			else if (num != -1)
			{
				if (!float.TryParse(json.Substring(num, i - num), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					heightmap = null;
					return false;
				}
				list.Add(result);
				num = -1;
			}
		}
		heightmap = new HeightMap
		{
			points = list.ToArray()
		};
		return true;
	}

	private Dictionary<string, Planet> CreatePlanets(Dictionary<string, PlanetData> planets, I_MsgLogger log, out bool success)
	{
		Dictionary<string, Planet> dictionary = new Dictionary<string, Planet>();
		foreach (KeyValuePair<string, PlanetData> planet2 in planets)
		{
			try
			{
				Transform obj = new GameObject(planet2.Key).transform;
				obj.parent = base.transform;
				Planet planet = obj.gameObject.AddComponent<Planet>();
				planet.SetupData(planet2.Key, planet2.Value, terrainShader, atmosphereShader, log);
				dictionary.Add(planet2.Key, planet);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				log.Log("ERROR: creating planet from loaded data: " + planet2.Key);
				success = false;
				return dictionary;
			}
		}
		foreach (Planet value in dictionary.Values)
		{
			try
			{
				value.SetupInteractions(dictionary);
			}
			catch
			{
				log.Log("ERROR: finding parent/satellite: " + value.codeName);
				success = false;
				return dictionary;
			}
		}
		foreach (Planet value2 in dictionary.Values)
		{
			try
			{
				value2.SetupDepthAndSatelliteIndex();
			}
			catch
			{
				log.Log("ERROR: finding satellite index/depth of " + value2.codeName);
				success = false;
				return dictionary;
			}
		}
		success = true;
		return dictionary;
	}

	public Texture2D GetTexture(string name, I_MsgLogger log)
	{
		if (textures.ContainsKey(name))
		{
			return textures[name];
		}
		log.Log("ERROR: cant find texture: " + name);
		return new Texture2D(1, 1);
	}

	public HeightMap GetHeightMap(string name, I_MsgLogger log)
	{
		if (heightmaps.ContainsKey(name))
		{
			return heightmaps[name];
		}
		log.Log("ERROR: Cant find heightmap: " + name);
		return new HeightMap(new float[2]);
	}
}
