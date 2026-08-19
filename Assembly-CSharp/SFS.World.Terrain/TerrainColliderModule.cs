using System;
using System.Collections.Generic;
using System.Linq;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain;

public class TerrainColliderModule : MonoBehaviour
{
	public Player player;

	public WorldLoader loader;

	private List<int> chunkIndexes = new List<int>();

	private void Start()
	{
		player.location.position.OnChange += new Action(UpdateChunks);
		loader.onLoadedChange_After += delegate
		{
			UpdateChunks();
		};
	}

	private void OnDestroy()
	{
		Clear();
	}

	private void UpdateChunks()
	{
		if (!loader.Loaded || !player.location.position.Value.Mag_LessThan(player.location.planet.Value.Radius + player.location.planet.Value.maxTerrainHeight + (double)player.GetSizeRadius()))
		{
			Clear();
			return;
		}
		Planet value = player.location.planet.Value;
		double num = (player.location.position.Value.AngleRadians / (Math.PI * 2.0) + 10.0) % 1.0;
		double num2 = (double)(player.GetSizeRadius() + 5f) / value.SurfaceArea;
		double angularChunkSize = TerrainColliderManager.GetAngularChunkSize(value);
		int num3 = (int)((num - num2) / angularChunkSize + 100.0) - 100;
		int num4 = (int)((num + num2) / angularChunkSize + 100.0) - 100;
		List<int> chunkIndexes_New = new List<int>();
		for (int i = num3; i <= num4; i++)
		{
			chunkIndexes_New.Add(i);
		}
		List<int> indexes = chunkIndexes.Where((int a) => !chunkIndexes_New.Contains(a)).ToList();
		List<int> indexes2 = chunkIndexes_New.Where((int a) => !chunkIndexes.Contains(a)).ToList();
		chunkIndexes = chunkIndexes_New;
		TerrainColliderManager.main.RemoveChunks(indexes, this);
		TerrainColliderManager.main.AddChunks(indexes2, this);
	}

	private void Clear()
	{
		if (chunkIndexes.Count != 0)
		{
			TerrainColliderManager.main.RemoveChunks(chunkIndexes, this);
			chunkIndexes.Clear();
		}
	}
}
