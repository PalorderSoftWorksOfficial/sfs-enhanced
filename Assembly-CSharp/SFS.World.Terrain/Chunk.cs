using System;
using System.Collections.Generic;
using System.Linq;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain;

[Serializable]
public class Chunk
{
	public class TerrainPoints
	{
		public Vector2[] points;

		public Vector2[] uvOthers;

		public TerrainPoints(int pointCount)
		{
			points = new Vector2[pointCount];
			uvOthers = new Vector2[pointCount];
		}
	}

	public Transform transform;

	public Mesh mesh;

	public Chunk(Transform chunkPrefab, double from, double size, int pointCount, Planet planet, Material material, bool useTerrainUV, Transform parent, bool offsetTerrain)
	{
		transform = UnityEngine.Object.Instantiate(chunkPrefab, parent).transform;
		TerrainPoints terrainPoints = planet.data.terrain.GetTerrainPoints(from, size, pointCount, offsetTerrain, forCollider: false, planet, out var offsetAmount);
		int[] indices = GetIndices(terrainPoints.points.Length);
		GenerateMesh(terrainPoints, indices, "Default", 10, material, planet);
		if (useTerrainUV)
		{
			GenerateMeshUV(terrainPoints, from, GetAngleBetweenPoints(size, pointCount), planet);
		}
		transform.localPosition = offsetAmount;
	}

	private void GenerateMesh(TerrainPoints terrainPoints, int[] indices, string sortingLayer, int sortingOrder, Material material, Planet planet)
	{
		mesh = transform.GetComponent<MeshFilter>().mesh;
		mesh.name = "ChunkMesh: " + planet.codeName;
		mesh.Clear();
		mesh.vertices = ((IEnumerable<Vector2>)terrainPoints.points).Select((Func<Vector2, Vector3>)((Vector2 point) => point)).ToArray();
		mesh.triangles = indices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		Renderer component = transform.GetComponent<Renderer>();
		component.sortingLayerName = sortingLayer;
		component.sortingOrder = sortingOrder;
		component.material = material;
	}

	private void GenerateMeshUV(TerrainPoints terrainPoints, double from, double angleBetweenPoints, Planet planet)
	{
		mesh = transform.GetComponent<MeshFilter>().mesh;
		Vector2[] array = new Vector2[terrainPoints.points.Length];
		for (int i = 0; i < terrainPoints.points.Length - 1; i++)
		{
			array[i + 1] = new Vector2((float)(from * Math.PI * 2.0 + angleBetweenPoints * (double)i), 0f);
		}
		array[0] = Vector3.up;
		mesh.uv = array;
		Vector2[] array2 = new Vector2[terrainPoints.points.Length];
		for (int j = 0; j < terrainPoints.points.Length; j++)
		{
			array2[j] = (terrainPoints.points[j] - terrainPoints.points[0]).normalized * (planet.data.terrain.TERRAIN_TEXTURE_DATA.planetTextureCutout * 0.5f) + new Vector2(0.5f, 0.5f);
		}
		mesh.uv2 = array2;
		mesh.uv3 = terrainPoints.uvOthers;
	}

	private int[] GetIndices(int length)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < length - 2; i++)
		{
			list.Add(0);
			list.Add(i + 2);
			list.Add(i + 1);
		}
		return list.ToArray();
	}

	public static double GetAngleBetweenPoints(double size, int pointCount)
	{
		return size * Math.PI * 2.0 / (double)(pointCount - 1);
	}
}
