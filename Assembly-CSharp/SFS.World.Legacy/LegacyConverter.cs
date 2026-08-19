using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SFS.Logs;
using SFS.Parsers.Json;
using SFS.Parsers.Regex;
using SFS.World.PlanetModules;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Legacy;

public static class LegacyConverter
{
	public static PlanetData CheckAndConvert_Planet(string name, string jsonText, I_MsgLogger log, out bool converted, out bool success)
	{
		if (!jsonText.Contains("\"version\": 1.5,") && !jsonText.Contains("\"version\": \"1.5\",") && !jsonText.Contains("\"version\":1.5,") && !jsonText.Contains("\"version\":\"1.5\","))
		{
			try
			{
				PlanetData result = Convert_Planet(FromJson_Old(jsonText));
				converted = true;
				success = true;
				return result;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				log.Log("ERROR: json format or legacy conversion " + name);
				success = false;
				converted = false;
				return null;
			}
		}
		converted = false;
		success = true;
		try
		{
			return JsonWrapper.FromJson<PlanetData>(jsonText);
		}
		catch
		{
			log.Log("ERROR: json format: " + name);
			success = false;
			return null;
		}
	}

	private static PlanetData_Old FromJson_Old(string jsonData)
	{
		List<string> list = new List<string>(jsonData.Split("●"[0], StringSplitOptions.None));
		PlanetData_Old planetData_Old = new PlanetData_Old();
		planetData_Old.BASE_DATA = JsonUtility.FromJson<PlanetData_Old.BasicModule>(list[list.IndexOf("BASE_DATA") + 1]);
		planetData_Old.hasAtmosphere = list.Contains("ATMOSPHERE_DATA");
		if (planetData_Old.hasAtmosphere)
		{
			planetData_Old.ATMOSPHERE_DATA = JsonUtility.FromJson<PlanetData_Old.AtmosphereModule_Old>(list[list.IndexOf("ATMOSPHERE_DATA") + 1]);
		}
		planetData_Old.hasPostProcessing = list.Contains("POST_PROCESSING");
		if (planetData_Old.hasPostProcessing)
		{
			planetData_Old.POST_PROCESSING = JsonUtility.FromJson<PlanetData_Old.ColorGradingGradient_Old>(list[list.IndexOf("POST_PROCESSING") + 1]);
		}
		planetData_Old.hasTerrain = list.Contains("TERRAIN_DATA");
		if (planetData_Old.hasTerrain)
		{
			planetData_Old.TERRAIN_DATA = JsonUtility.FromJson<PlanetData_Old.TerrainModule_Old>(list[list.IndexOf("TERRAIN_DATA") + 1]);
		}
		planetData_Old.hasOrbitData = list.Contains("ORBIT_DATA");
		if (planetData_Old.hasOrbitData)
		{
			planetData_Old.ORBIT_DATA = JsonUtility.FromJson<PlanetData_Old.OrbitModule_Old>(list[list.IndexOf("ORBIT_DATA") + 1]);
		}
		return planetData_Old;
	}

	private static PlanetData Convert_Planet(PlanetData_Old oldData)
	{
		if (oldData.hasTerrain)
		{
			oldData.TERRAIN_DATA.terrainFromula = Convert_TerrainFormula(oldData, oldData.TERRAIN_DATA.terrainFromula);
			oldData.TERRAIN_DATA.textureFormula = Convert_TerrainFormula(oldData, oldData.TERRAIN_DATA.textureFormula);
		}
		return new PlanetData
		{
			hasAtmospherePhysics = (oldData.hasAtmosphere && oldData.ATMOSPHERE_DATA.PHYSICS.height > 1.0),
			hasAtmosphereVisuals = oldData.hasAtmosphere,
			hasOrbit = oldData.hasOrbitData,
			hasPostProcessing = oldData.hasPostProcessing,
			hasTerrain = oldData.hasTerrain,
			basics = Convert_Basic(oldData.BASE_DATA),
			atmospherePhysics = (oldData.hasAtmosphere ? Convert_Atmosphere_Physics(oldData.ATMOSPHERE_DATA) : null),
			atmosphereVisuals = (oldData.hasAtmosphere ? Convert_Atmosphere_Visuals(oldData.ATMOSPHERE_DATA, oldData) : null),
			orbit = (oldData.hasOrbitData ? Convert_Orbit(oldData.ORBIT_DATA) : null),
			postProcessing = (oldData.hasPostProcessing ? Convert_PostProcessing(oldData.POST_PROCESSING) : null),
			terrain = (oldData.hasTerrain ? Convert_Terrain(oldData.TERRAIN_DATA) : null),
			logs = new LogsModule()
		};
	}

	private static string[] Convert_TerrainFormula(PlanetData_Old oldData, string[] formula)
	{
		SimpleRegex simpleRegex = new SimpleRegex("AddHeightMap\\( *\\S*,(?<repeat> *\\d*\\.*\\d*)");
		List<string> list = new List<string>();
		foreach (string text in formula)
		{
			if (simpleRegex.Input(text))
			{
				Group obj = simpleRegex.GetGroup("repeat");
				string text2 = text.Remove(obj.Index, obj.Length);
				if (!double.TryParse(obj.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					Debug.LogWarning("Could not parse float! " + obj.Value);
					list.Add(text);
				}
				else
				{
					double num = oldData.BASE_DATA.radius * Math.PI * 2.0 / Math.Max(result, 0.01);
					text2 = text2.Insert(obj.Index, num.ToString(CultureInfo.InvariantCulture));
					list.Add(text2);
				}
			}
			else
			{
				list.Add(text);
			}
		}
		return list.ToArray();
	}

	private static TerrainModule Convert_Terrain(PlanetData_Old.TerrainModule_Old oldData)
	{
		if (oldData == null)
		{
			return null;
		}
		PlanetData_Old.TerrainModule_Old.TerrainTexture_Old tERRAIN_TEXTURE_DATA = oldData.TERRAIN_TEXTURE_DATA;
		return new TerrainModule
		{
			terrainFormulaDifficulties = new Dictionary<SFS.WorldBase.Difficulty.DifficultyType, string[]> { 
			{
				SFS.WorldBase.Difficulty.DifficultyType.Normal,
				oldData.terrainFromula
			} },
			TERRAIN_TEXTURE_DATA = new TerrainModule.TerrainTexture
			{
				maxFade = tERRAIN_TEXTURE_DATA.maxFade,
				minFade = tERRAIN_TEXTURE_DATA.minFade,
				planetTexture = tERRAIN_TEXTURE_DATA.planetTexture,
				planetTextureCutout = tERRAIN_TEXTURE_DATA.planetTextureCutout,
				shadowHeight = tERRAIN_TEXTURE_DATA.shadowHeight,
				shadowIntensity = tERRAIN_TEXTURE_DATA.shadowIntensity,
				surfaceLayerSize = tERRAIN_TEXTURE_DATA.surfaceLayerSize,
				surfaceTextureSize_A = tERRAIN_TEXTURE_DATA.surfaceTextureSizeA.ToVector2(),
				surfaceTextureSize_B = tERRAIN_TEXTURE_DATA.surfaceTextureSizeB.ToVector2(),
				terrainTextureSize_C = tERRAIN_TEXTURE_DATA.terrainTextureSize.ToVector2(),
				surfaceTexture_A = tERRAIN_TEXTURE_DATA.surfaceTextureA,
				surfaceTexture_B = tERRAIN_TEXTURE_DATA.surfaceTextureB,
				terrainTexture_C = tERRAIN_TEXTURE_DATA.terrainTexture
			},
			textureFormula = oldData.textureFormula,
			verticeSize = oldData.DETAIL_LEVELS.Last().verticeSize
		};
	}

	private static PostProcessingModule Convert_PostProcessing(PlanetData_Old.ColorGradingGradient_Old oldData)
	{
		if (oldData == null)
		{
			return null;
		}
		return new PostProcessingModule
		{
			keys = oldData.keys.Select((PlanetData_Old.ColorGradingGradient_Old.Key oldKey) => new PostProcessingModule.Key(oldKey.height, oldKey.shadowIntensity, 1f, oldKey.hueShift, oldKey.saturation, oldKey.contrast, oldKey.red, oldKey.green, oldKey.blue)).ToArray()
		};
	}

	private static OrbitModule Convert_Orbit(PlanetData_Old.OrbitModule_Old oldData)
	{
		if (oldData == null)
		{
			return null;
		}
		return new OrbitModule
		{
			semiMajorAxis = oldData.orbitHeight,
			eccentricity = oldData.eccentricity,
			argumentOfPeriapsis = oldData.argumentOfPeriapsis,
			multiplierSOI = oldData.multiplierSOI,
			parent = oldData.parent
		};
	}

	private static BasicModule Convert_Basic(PlanetData_Old.BasicModule oldData)
	{
		if (oldData == null)
		{
			return null;
		}
		return new BasicModule
		{
			gravity = oldData.gravity,
			mapColor = new Color(oldData.mapColor.r, oldData.mapColor.g, oldData.mapColor.b),
			radius = oldData.radius,
			timewarpHeight = oldData.timewarpHeight
		};
	}

	private static Atmosphere_Physics Convert_Atmosphere_Physics(PlanetData_Old.AtmosphereModule_Old oldData)
	{
		if (oldData == null)
		{
			return null;
		}
		return new Atmosphere_Physics
		{
			curve = oldData.PHYSICS.curve,
			density = oldData.PHYSICS.density,
			height = oldData.PHYSICS.height
		};
	}

	private static Atmosphere_Visuals Convert_Atmosphere_Visuals(PlanetData_Old.AtmosphereModule_Old oldData, PlanetData_Old oldPlanet)
	{
		if (oldData == null)
		{
			return null;
		}
		List<Atmosphere_Visuals.ColorGradient.Key> list = new List<Atmosphere_Visuals.ColorGradient.Key>();
		if (oldData.FOG != null && oldData.FOG.keys != null)
		{
			list.AddRange(oldData.FOG.keys.Select((PlanetData_Old.AtmosphereModule_Old.SerializableGradient_Old.Key oldFogKey) => new Atmosphere_Visuals.ColorGradient.Key(oldFogKey.GetColor(), oldFogKey.distance)));
		}
		return new Atmosphere_Visuals
		{
			GRADIENT = new Atmosphere_Visuals.Gradient
			{
				height = oldData.GRADIENT.gradientHeight,
				texture = oldData.GRADIENT.gradientTexture,
				positionZ = oldData.GRADIENT.positionZ
			},
			CLOUDS = new Atmosphere_Visuals.Clouds
			{
				alpha = oldData.CLOUDS.alpha,
				texture = oldData.CLOUDS.cloudTexture,
				velocity = oldData.CLOUDS.cloudVelocity,
				height = oldData.CLOUDS.height,
				width = (float)((oldPlanet.BASE_DATA.radius + (double)oldData.CLOUDS.startHeight) * (Math.PI * 2.0)) / oldData.CLOUDS.repeatX / 256f,
				startHeight = oldData.CLOUDS.startHeight
			},
			FOG = new Atmosphere_Visuals.ColorGradient
			{
				keys = list.ToArray()
			}
		};
	}
}
