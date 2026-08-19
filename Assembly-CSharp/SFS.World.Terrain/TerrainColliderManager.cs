using System;
using System.Collections.Generic;
using SFS.Variables;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain;

public class TerrainColliderManager : MonoBehaviour
{
	[Serializable]
	public class Chunk
	{
		public List<TerrainColliderModule> owners = new List<TerrainColliderModule>();

		private GameObject gameObject;

		private Double2 position;

		public Chunk(double from, double size, Planet planet, Transform holder)
		{
			gameObject = new GameObject("Collider");
			gameObject.transform.SetParent(holder, worldPositionStays: false);
			gameObject.layer = holder.gameObject.layer;
			if (planet.data.hasTerrain && planet.data.terrain.collider)
			{
				SFS.World.Terrain.Chunk.TerrainPoints terrainPoints = planet.data.terrain.GetTerrainPoints(from, size, 11, offset: true, forCollider: true, planet, out position);
				gameObject.AddComponent<PolygonCollider2D>().points = terrainPoints.points;
			}
			WorldView.main.positionOffset.OnChange += new Action(Position);
		}

		public void Position()
		{
			gameObject.transform.position = WorldView.ToLocalPosition(position);
		}

		public void Destroy()
		{
			WorldView.main.positionOffset.OnChange -= new Action(Position);
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	public static TerrainColliderManager main;

	private Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>();

	public Bool_Local hasColliders;

	private const int VerticesPerChunk = 10;

	private void Awake()
	{
		main = this;
	}

	public void AddChunks(List<int> indexes, TerrainColliderModule owner)
	{
		foreach (int index in indexes)
		{
			if (!chunks.ContainsKey(index))
			{
				Planet value = owner.player.location.planet.Value;
				double angularChunkSize = GetAngularChunkSize(value);
				chunks[index] = new Chunk(angularChunkSize * (double)index, angularChunkSize, value, base.transform);
			}
			chunks[index].owners.Add(owner);
		}
		if (chunks.Count > 0)
		{
			hasColliders.Value = true;
		}
	}

	public void RemoveChunks(List<int> indexes, TerrainColliderModule owner)
	{
		foreach (int index in indexes)
		{
			if (!chunks.ContainsKey(index))
			{
				Debug.LogError("Couldn't remove chunk: " + index);
				continue;
			}
			chunks[index].owners.Remove(owner);
			if (chunks[index].owners.Count <= 0)
			{
				chunks[index].Destroy();
				chunks.Remove(index);
			}
		}
		if (chunks.Count == 0)
		{
			hasColliders.Value = false;
		}
	}

	public static double GetAngularChunkSize(Planet planet)
	{
		double chunkSize_Angular = planet.data.terrain.GetChunkSize_Angular(8, planet.GetMaxLOD());
		int verticeCount = planet.GetVerticeCount(chunkSize_Angular, planet.data.terrain.verticeSize);
		return chunkSize_Angular / (double)(verticeCount - 1) * 10.0;
	}
}
