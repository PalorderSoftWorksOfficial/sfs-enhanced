using System;
using System.Linq;
using SFS.World.Terrain;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class MapEnvironment : MonoBehaviour
{
	public Sprite white_Sprite;

	public Sprite SOI_Sprite;

	[Space]
	public Shader terrainShader;

	public Transform chunkPrefab;

	private MapPlanetEnvironment[] environments;

	public void CreateReferences()
	{
		foreach (Planet value in Base.planetLoader.planets.Values)
		{
			value.mapPlanet = base.gameObject.AddComponent<MapPlanet>();
			value.mapPlanet.planet = value;
		}
	}

	public void CreateEnvironments()
	{
		Double3 cameraPosition = Map.view.GetCameraPosition();
		environments = Base.planetLoader.planets.Values.Select((Planet planet) => CreateEnvironment(planet, cameraPosition)).ToArray();
	}

	private MapPlanetEnvironment CreateEnvironment(Planet planet, Double3 cameraPosition)
	{
		MapPlanetEnvironment mapPlanetEnvironment = new MapPlanetEnvironment();
		mapPlanetEnvironment.planet = planet;
		mapPlanetEnvironment.holder = new GameObject(planet.codeName).transform;
		mapPlanetEnvironment.holder.parent = base.transform;
		planet.mapHolder = mapPlanetEnvironment.holder;
		if (planet.data.hasTerrain)
		{
			mapPlanetEnvironment.terrain = CreateTerrain(planet, (Double3)planet.GetSolarSystemPosition() - cameraPosition, mapPlanetEnvironment.holder);
		}
		else
		{
			CreateCircle((float)(planet.Radius / 1000.0), mapPlanetEnvironment.holder, white_Sprite, planet.data.basics.mapColor, 1, "Terrain");
		}
		if (planet.HasAtmospherePhysics)
		{
			CreateCircle((float)((planet.Radius + planet.AtmosphereHeightPhysics) / 1000.0), mapPlanetEnvironment.holder, white_Sprite, new Color(1f, 1f, 1f, 0.1f), 2, "Atmosphere");
		}
		if (planet.HasParent)
		{
			CreateCircle((float)(planet.SOI / 1000.0), mapPlanetEnvironment.holder, SOI_Sprite, new Color(1f, 1f, 1f, 0.08f), 1, "SOI");
		}
		return mapPlanetEnvironment;
	}

	private static void CreateCircle(float radius, Transform parent, Sprite sprite, Color color, int sortingOrder, string name)
	{
		Transform obj = new GameObject(name).transform;
		obj.gameObject.layer = LayerMask.NameToLayer("Map");
		obj.parent = parent;
		obj.localPosition = Vector3.zero;
		obj.localScale = Vector3.one * radius;
		SpriteRenderer spriteRenderer = obj.gameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = sprite;
		spriteRenderer.color = color;
		spriteRenderer.sortingLayerName = "Map";
		spriteRenderer.sortingOrder = sortingOrder;
	}

	private DynamicTerrain CreateTerrain(Planet planet, Double3 positionToCamera, Transform parent)
	{
		if (terrainShader == null)
		{
			throw new Exception("SHADER NULL...");
		}
		Material material = new Material(terrainShader)
		{
			color = planet.data.basics.mapColor
		};
		DynamicTerrain dynamicTerrain = DynamicTerrain.Create(planet, new Double3(0.0 - positionToCamera.x, 0.0 - positionToCamera.y, positionToCamera.z), SetupChunk, 4f, 0.6f, "Map", material, useTerrainUV: false, chunkPrefab, null, null);
		dynamicTerrain.transform.parent = parent;
		dynamicTerrain.transform.position = Vector3.zero;
		dynamicTerrain.transform.localScale = Vector3.one / 1000f;
		return dynamicTerrain;
	}

	private static void SetupChunk(DynamicTerrain.DynamicChunk a)
	{
		MeshRenderer component = a.chunk.transform.GetComponent<MeshRenderer>();
		component.sortingOrder = 10;
		component.sortingLayerName = "Map";
	}

	public void PositionPlanets()
	{
		Double3 cameraPosition = Map.view.GetCameraPosition();
		MapPlanetEnvironment[] array = environments;
		foreach (MapPlanetEnvironment mapPlanetEnvironment in array)
		{
			Double3 @double = (Double3)mapPlanetEnvironment.planet.GetSolarSystemPosition() - cameraPosition;
			mapPlanetEnvironment.holder.localPosition = @double / 1000.0;
			if (mapPlanetEnvironment.terrain != null)
			{
				mapPlanetEnvironment.terrain.SetViewPosition(new Double3(0.0 - @double.x, 0.0 - @double.y, @double.z));
			}
		}
	}
}
