using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Translations;
using SFS.World;
using SFS.World.Maps;
using SFS.World.PlanetModules;
using UnityEngine;

namespace SFS.WorldBase;

public class Planet : MonoBehaviour
{
	public string codeName;

	public Transform mapHolder;

	public MapPlanet mapPlanet;

	public Landmark[] landmarks;

	public Trajectory trajectory;

	public Orbit orbit;

	public Planet parentBody;

	public Planet[] satellites;

	public double mass;

	public double SOI;

	public double maxTerrainHeight;

	public Texture2D planetTexture;

	public Material terrainMaterial;

	public Material atmosphereMaterial;

	public int orbitalDepth;

	public int satelliteIndex;

	public PlanetData data;

	public double Radius => data.basics.radius;

	public double SurfaceArea => Radius * (Math.PI * 2.0);

	public double AtmosphereHeightPhysics
	{
		get
		{
			if (!HasAtmospherePhysics)
			{
				return double.NegativeInfinity;
			}
			return data.atmospherePhysics.height;
		}
	}

	public double TimewarpRadius_Ascend => Radius + data.basics.timewarpHeight.Round(Math.Pow(10.0, Math.Floor(Math.Log10(data.basics.timewarpHeight / 2.0))) / 2.0);

	public double TimewarpRadius_Descend => Radius + Math.Max(maxTerrainHeight, Math.Max(data.basics.timewarpHeight, AtmosphereHeightPhysics));

	public double OrbitRadius => Radius + Math.Max(AtmosphereHeightPhysics, maxTerrainHeight);

	public bool HasParent => parentBody != null;

	public bool HasAtmospherePhysics => data.hasAtmospherePhysics;

	public bool HasAtmosphereVisuals => data.hasAtmosphereVisuals;

	public double RewardMultiplier => 1.0;

	public Field DisplayName { get; private set; }

	private void OnStart()
	{
		Loc.OnChange += new Action(UpdateName);
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(UpdateName);
		if (terrainMaterial != null)
		{
			UnityEngine.Object.Destroy(terrainMaterial);
		}
		if (atmosphereMaterial != null)
		{
			UnityEngine.Object.Destroy(atmosphereMaterial);
		}
	}

	private void UpdateName()
	{
		DisplayName = codeName switch
		{
			"Sun" => Loc.main.Sun, 
			"Mercury" => Loc.main.Mercury, 
			"Venus" => Loc.main.Venus, 
			"Earth" => Loc.main.Earth, 
			"Moon" => Loc.main.Moon, 
			"Near_Earth_Asteroid" => Loc.main.Near_Earth_Asteroid, 
			"Mars" => Loc.main.Mars, 
			"Phobos" => Loc.main.Phobos, 
			"Deimos" => Loc.main.Deimos, 
			"Jupiter" => Loc.main.Jupiter, 
			"Io" => Loc.main.Io, 
			"Europa" => Loc.main.Europa, 
			"Ganymede" => Loc.main.Ganymede, 
			"Callisto" => Loc.main.Callisto, 
			_ => Field.Text(Application.isEditor ? ("*" + codeName) : codeName), 
		};
	}

	public double GetGravity(double radius)
	{
		return mass / (radius * radius);
	}

	public Double2 GetGravity(Double2 position)
	{
		return -position.normalized * (mass / position.sqrMagnitude);
	}

	public double GetEscapeVelocity(double radius)
	{
		return Math.Sqrt(2.0 * mass / radius);
	}

	public Location GetLocation(double time)
	{
		if (!(parentBody != null))
		{
			return new Location(time, this, Double2.zero, Double2.zero);
		}
		return orbit.GetLocation(time);
	}

	public Double2 GetSolarSystemPosition()
	{
		return GetSolarSystemPosition(WorldTime.main.worldTime);
	}

	public Double2 GetSolarSystemPosition(double time)
	{
		if (!data.hasOrbit)
		{
			return Double2.zero;
		}
		return GetLocation(time).position + parentBody.GetSolarSystemPosition(time);
	}

	public int GetVerticeCount(double size_Angular, double verticeSize)
	{
		return Math.Max((int)(SurfaceArea * size_Angular / verticeSize), 10);
	}

	public int GetMaxLOD()
	{
		int num = 0;
		double num2 = Radius * (Math.PI * 2.0) / 8.0;
		num2 /= 2.0;
		num++;
		while (num2 > 120.0)
		{
			num2 /= 2.0;
			num++;
		}
		return num;
	}

	public bool IsInsideTerrain(Double2 position, double threshold)
	{
		if (position.Mag_MoreThan(Radius + maxTerrainHeight))
		{
			return false;
		}
		double a = Radius + GetTerrainHeightAtAngle(position.AngleRadians) - threshold;
		return position.Mag_LessThan(a);
	}

	public Double2 GetTerrainNormal(Double2 globalPosition)
	{
		double angleRadians = globalPosition.AngleRadians;
		double num = 0.1 / SurfaceArea;
		double angleRadians2 = angleRadians + num;
		double angleRadians3 = angleRadians - num;
		Double2 @double = Double2.CosSin(angleRadians2, Radius + GetTerrainHeightAtAngle(angleRadians2));
		return (Double2.CosSin(angleRadians3, Radius + GetTerrainHeightAtAngle(angleRadians3)) - @double).normalized;
	}

	public double GetTerrainHeightAtAngle(double angleRadians)
	{
		return GetTerrainHeightAtAngles(angleRadians)[0];
	}

	public float[] GetTerrainNormals(params double[] angles_Radians)
	{
		double num = 0.5 / SurfaceArea;
		float[] array = new float[angles_Radians.Length];
		double[] array2 = new double[angles_Radians.Length * 2];
		for (int i = 0; i < angles_Radians.Length; i++)
		{
			double num2 = angles_Radians[i];
			double num3 = num2 + num;
			double num4 = num2 - num;
			array2[i * 2] = num3;
			array2[i * 2 + 1] = num4;
		}
		double[] terrainHeightAtAngles = GetTerrainHeightAtAngles(array2);
		for (int j = 0; j < angles_Radians.Length; j++)
		{
			double num5 = angles_Radians[j];
			double angleRadians = num5 + num;
			double angleRadians2 = num5 - num;
			Double2 @double = Double2.CosSin(angleRadians, Radius + terrainHeightAtAngles[j * 2]);
			Double2 double2 = Double2.CosSin(angleRadians2, Radius + terrainHeightAtAngles[j * 2 + 1]);
			array[j] = (float)(double2 - @double).AngleDegrees;
		}
		return array;
	}

	public double[] GetTerrainHeightAtAngles(params double[] angleRadians)
	{
		double[] array = new double[angleRadians.Length];
		for (int i = 0; i < angleRadians.Length; i++)
		{
			array[i] = Kepler.PositiveAngle(angleRadians[i]) / (Math.PI * 2.0);
		}
		if (!data.hasTerrain)
		{
			return new double[array.Length];
		}
		return TerrainSampler.GetTerrainSamples(data, array, 0.0, 1.0);
	}

	public bool IsInsideAtmosphere(Double2 position)
	{
		if (!data.hasAtmospherePhysics)
		{
			return false;
		}
		double a = data.basics.radius + data.atmospherePhysics.height;
		return position.Mag_LessThan(a);
	}

	public double GetAtmosphericDensity(double height)
	{
		if (!data.hasAtmospherePhysics)
		{
			return 0.0;
		}
		if (height > AtmosphereHeightPhysics)
		{
			return 0.0;
		}
		return (Math.Exp(height / AtmosphereHeightPhysics * (0.0 - data.atmospherePhysics.curve)) - Math.Exp(0.0 - data.atmospherePhysics.curve)) * data.atmospherePhysics.density;
	}

	public bool IsOutsideSOI(Double2 position)
	{
		return position.Mag_MoreThan(SOI);
	}

	public bool IsInsideSOI(Double2 positionToParent)
	{
		return (positionToParent - orbit.GetLocation(WorldTime.main.worldTime).position).Mag_LessThan(SOI);
	}

	public static double GetTimewarpRadius_AscendDescend(Location location)
	{
		if (!(Double2.Dot(location.position, location.velocity) > 0.0))
		{
			return location.planet.TimewarpRadius_Descend;
		}
		return location.planet.TimewarpRadius_Ascend;
	}

	public void SetupData(string codeName, PlanetData data, Shader terrainShader, Shader atmosphereShader, I_MsgLogger log)
	{
		this.codeName = codeName;
		this.data = data;
		OnStart();
		mass = Kepler.GetMass(data.basics.gravity, Radius);
		if (data.hasTerrain)
		{
			terrainMaterial = CreateTerrainMaterial(data.terrain.TERRAIN_TEXTURE_DATA, terrainShader, log);
			data.terrain.SetupSamplers(codeName, log);
			maxTerrainHeight = data.terrain.GetMaxTerrainHeight(data) + 200.0;
		}
		if (data.landmarks != null)
		{
			landmarks = data.landmarks.Select(delegate(LandmarkData landmarkData)
			{
				Landmark landmark = base.gameObject.AddComponent<Landmark>();
				landmark.Initialize(landmarkData, this);
				return landmark;
			}).ToArray();
		}
		else
		{
			landmarks = new Landmark[0];
		}
		if (data.hasAtmosphereVisuals)
		{
			atmosphereMaterial = CreateAtmosphereMaterial(data.atmosphereVisuals, atmosphereShader, log);
		}
	}

	private Material CreateAtmosphereMaterial(Atmosphere_Visuals input, Shader shader, I_MsgLogger log)
	{
		Material material = new Material(shader);
		Texture2D texture = Base.planetLoader.GetTexture(input.GRADIENT.texture, log);
		texture.wrapMode = TextureWrapMode.Clamp;
		material.SetTexture("_GradientTex", texture);
		material.SetFloat("_GradientMultiplier", (float)((Radius + input.GRADIENT.height) / input.GRADIENT.height) - 1f);
		Texture2D texture2 = Base.planetLoader.GetTexture(input.CLOUDS.texture, log);
		material.SetTexture("_CouldTex", texture2);
		material.SetFloat("_CloudStartY", (float)((Radius + (double)input.CLOUDS.startHeight + input.GRADIENT.height) / input.GRADIENT.height) - 1f);
		material.SetFloat("_CloudSizeY", (float)(Radius + input.GRADIENT.height) / input.CLOUDS.height);
		material.SetFloat("_CloudSizeX", (float)((Radius + (double)input.CLOUDS.startHeight) * (Math.PI * 2.0)) / input.CLOUDS.width);
		material.SetFloat("_Alpha", input.CLOUDS.alpha);
		return material;
	}

	private Material CreateTerrainMaterial(TerrainModule.TerrainTexture input, Shader shader, I_MsgLogger log)
	{
		Material material = new Material(shader);
		planetTexture = Base.planetLoader.GetTexture(input.planetTexture, log);
		material.SetTexture("_PlanetTexture", planetTexture);
		material.SetTexture("_TextureA", Base.planetLoader.GetTexture(input.surfaceTexture_A, log));
		material.SetTexture("_TextureB", Base.planetLoader.GetTexture(input.surfaceTexture_B, log));
		material.SetTexture("_TextureTerrain", Base.planetLoader.GetTexture(input.terrainTexture_C, log));
		material.SetVector("_RepeatA", (Vector2)GetRepeat(input.surfaceTextureSize_A * new Vector2(4.712389f, 1f)));
		material.SetVector("_RepeatB", (Vector2)GetRepeat(input.surfaceTextureSize_B * new Vector2(4.712389f, 1f)));
		material.SetVector("_RepeatTerrain", (Vector2)GetRepeat(input.terrainTextureSize_C * new Vector2(4.712389f, 1f)));
		material.SetFloat("_SurfaceSize", (float)Radius / input.surfaceLayerSize);
		material.SetFloat("_Min", input.minFade);
		material.SetFloat("_Max", input.maxFade);
		material.SetFloat("_ShadowIntensity", input.shadowIntensity);
		material.SetFloat("_ShadowSize", (float)Radius / input.shadowHeight);
		material.SetColor("_Fog", Color.clear);
		return material;
	}

	private Vector2Int GetRepeat(Vector2 size)
	{
		return new Vector2Int((int)(SurfaceArea / (double)size.x), (int)(Radius / (double)size.y));
	}

	public void SetupDepthAndSatelliteIndex()
	{
		orbitalDepth = GetOrbitalDepth();
		satelliteIndex = (data.hasOrbit ? new List<Planet>(parentBody.satellites).IndexOf(this) : (-1));
	}

	private int GetOrbitalDepth()
	{
		if (!(parentBody != null))
		{
			return 0;
		}
		return parentBody.GetOrbitalDepth() + 1;
	}

	public void SetupInteractions(Dictionary<string, Planet> planets)
	{
		bool flag = data.hasOrbit && planets.ContainsKey(data.orbit.parent);
		parentBody = (flag ? planets[data.orbit.parent] : null);
		orbit = (flag ? new Orbit(data.orbit.semiMajorAxis, data.orbit.eccentricity, data.orbit.argumentOfPeriapsis * 0.01745329238474369, -data.orbit.direction, parentBody, PathType.Eternal, null) : null);
		trajectory = (flag ? new Trajectory(orbit) : Trajectory.Empty);
		SOI = (flag ? Kepler.GetSphereOfInfluence(data.orbit.semiMajorAxis, mass, parentBody.mass, data.orbit.multiplierSOI) : double.PositiveInfinity);
		satellites = GetSatellites(planets);
	}

	private Planet[] GetSatellites(Dictionary<string, Planet> planets)
	{
		List<Planet> list = planets.Values.Where((Planet planet) => planet.data.hasOrbit && planet.data.orbit.parent == codeName).ToList();
		list.Sort((Planet a, Planet b) => (a.data.orbit.semiMajorAxis > b.data.orbit.semiMajorAxis) ? 1 : (-1));
		return list.ToArray();
	}

	public Color GetTerrainColor(Double2 position)
	{
		Vector2 vector = (Vector2)position.normalized * (data.terrain.TERRAIN_TEXTURE_DATA.planetTextureCutout * 0.5f) + new Vector2(0.5f, 0.5f);
		return planetTexture.GetPixelBilinear(vector.x, vector.y);
	}
}
