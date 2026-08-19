using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Career;
using SFS.World.PlanetModules;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Terrain;

[Serializable]
public class DynamicTerrain : MonoBehaviour
{
	[Serializable]
	public class DynamicChunk
	{
		private DynamicTerrain owner;

		private DynamicChunk parentChunk;

		private DynamicChunk otherHalf;

		public Chunk chunk;

		private double topSizeHalf;

		private Double2 topPosition;

		private double from;

		private int LOD;

		public double updateSplit;

		public double updateMerge;

		private List<Transform> rocks = new List<Transform>();

		public DynamicChunk(DynamicTerrain owner, DynamicChunk parentChunk, double from, int LOD)
		{
			if (LOD == 0)
			{
				updateMerge = double.PositiveInfinity;
			}
			this.owner = owner;
			this.parentChunk = parentChunk;
			this.from = from;
			this.LOD = LOD;
			double chunkSize_Angular = owner.planet.data.terrain.GetChunkSize_Angular(8, LOD);
			double num = owner.planet.Radius + owner.planet.maxTerrainHeight;
			topSizeHalf = chunkSize_Angular * Math.PI * num;
			topPosition = Double2.CosSin((from + chunkSize_Angular / 2.0) * (Math.PI * 2.0), num);
			double verticeSize = owner.planet.data.terrain.GetVerticeSize(LOD, owner.planet.GetMaxLOD()) * (double)owner.verticeSizeMultiplier;
			int verticeCount = owner.planet.GetVerticeCount(chunkSize_Angular, verticeSize);
			chunk = new Chunk(owner.chunkPrefab, from, chunkSize_Angular, verticeCount, owner.planet, owner.material, owner.useTerrainUV, owner.transform, offsetTerrain: true);
			chunk.transform.name = "Chunk - LOD: " + LOD;
			owner.setupChunk?.Invoke(this);
			GenerateRocks();
			owner.allChunks.Add(this);
			EnableChunk();
		}

		private void GenerateRocks()
		{
			if (owner.rockData == null || LOD != owner.planet.GetMaxLOD())
			{
				return;
			}
			Planet planet = owner.planet;
			double chunkSize_Angular = owner.planet.data.terrain.GetChunkSize_Angular(8, LOD);
			int num = (int)(chunkSize_Angular * (Math.PI * 2.0) * planet.Radius * (double)owner.rockData.rockDensity * 2.0);
			float num2 = 1f / (float)num;
			System.Random random = new System.Random((int)(from * 100000.0));
			List<(double, float, float, float)> list = new List<(double, float, float, float)>();
			for (int i = 0; i < num; i++)
			{
				if (!(GetRandom() > 0.5f))
				{
					float num3 = ((float)i + GetRandom() * 3f) * num2;
					double num4 = (from + chunkSize_Angular * (double)num3) * (Math.PI * 2.0);
					Dictionary<string, HashSet<long>> collectedRocks = AstronautState.main.state.collectedRocks;
					if (collectedRocks.ContainsKey(planet.codeName) && collectedRocks[planet.codeName].Contains(RockSelector.GetRockID(num4, planet)))
					{
						GetRandom();
						GetRandom();
						GetRandom();
					}
					else
					{
						list.Add((num4, GetRandom(), GetRandom(), GetRandom()));
					}
				}
			}
			double[] array = list.Select<(double, float, float, float), double>(((double angle, float size, float angleOffset, float color) a) => a.angle).ToArray();
			double[] terrainHeightAtAngles = planet.GetTerrainHeightAtAngles(array);
			float[] terrainNormals = planet.GetTerrainNormals(array);
			for (int num5 = 0; num5 < list.Count; num5++)
			{
				(double, float, float, float) tuple = list[num5];
				TerrainModule.RockData rockData = owner.rockData;
				Transform transform = UnityEngine.Object.Instantiate(owner.rockPrefab, chunk.transform);
				rocks.Add(transform);
				Double2 @double = Double2.CosSin(tuple.Item1, planet.Radius + terrainHeightAtAngles[num5]);
				Vector2 vector = @double - chunk.transform.localPosition;
				transform.localPosition = vector;
				float num6 = Mathf.LerpUnclamped(rockData.minSize, rockData.maxSize, Mathf.Pow(tuple.Item2, rockData.powerCurve));
				transform.localScale = owner.rockPrefab.localScale * num6;
				float num7 = Mathf.LerpUnclamped(0f - rockData.maxAngle, rockData.maxAngle, tuple.Item3);
				transform.localEulerAngles = new Vector3(0f, 0f, terrainNormals[num5] + num7);
				float num8 = (float)tuple.Item1 - terrainNormals[num5] * ((float)Math.PI / 180f) - (float)Math.PI / 2f;
				float num9 = 1f + Mathf.Tan(0f - num8) * 0.8f;
				Color color = owner.planet.GetTerrainColor(@double) * num9 * Mathf.LerpUnclamped(0.9f, 1.05f, tuple.Item4);
				color.a = 1f;
				transform.GetChild(0).GetComponent<SpriteRenderer>().color = color;
			}
			float GetRandom()
			{
				return (float)random.NextDouble();
			}
		}

		public bool TrySplit(DynamicTerrain loader)
		{
			double num = GetDistanceToViewPosition(loader) - loader.planet.data.terrain.GetLoadDistance(LOD, loader.planet.GetMaxLOD()) * (double)owner.loadDistanceMultiplier;
			if (num < 0.0)
			{
				Split(out var firstHalf, out var secondHalf);
				if (LOD + 1 == owner.planet.GetMaxLOD())
				{
					firstHalf.updateSplit = double.PositiveInfinity;
					secondHalf.updateSplit = double.PositiveInfinity;
				}
				secondHalf.updateMerge = double.PositiveInfinity;
				return true;
			}
			updateSplit = loader.distanceMoved + num;
			return false;
		}

		public bool TryMerge(DynamicTerrain loader)
		{
			if (!CanMerge())
			{
				return false;
			}
			double num = parentChunk.GetDistanceToViewPosition(loader) - owner.planet.data.terrain.GetLoadDistance(LOD - 1, owner.planet.GetMaxLOD()) * (double)owner.loadDistanceMultiplier;
			if (num > 0.0)
			{
				Merge();
				return true;
			}
			updateMerge = loader.distanceMoved - num;
			return false;
		}

		public double GetDistanceToViewPosition(DynamicTerrain loader)
		{
			double closestPointOnLine = Math_Utility.GetClosestPointOnLine(Double2.zero, topPosition, (Double2)loader.position);
			closestPointOnLine = Math.Max(0.0, Math.Min(closestPointOnLine, 1.0));
			double num = topSizeHalf * closestPointOnLine;
			return ((Double3)topPosition * closestPointOnLine - loader.position).magnitude - num;
		}

		public bool CanMerge()
		{
			if (otherHalf != null && otherHalf.chunk != null)
			{
				return otherHalf.chunk.transform.gameObject.activeSelf;
			}
			return false;
		}

		public virtual void Split(out DynamicChunk firstHalf, out DynamicChunk secondHalf)
		{
			DisableChunk();
			firstHalf = new DynamicChunk(owner, this, from, LOD + 1);
			secondHalf = new DynamicChunk(owner, this, from + owner.planet.data.terrain.GetChunkSize_Angular(8, LOD + 1), LOD + 1);
			firstHalf.otherHalf = secondHalf;
		}

		public void Merge()
		{
			DestroyChunk();
			otherHalf.DestroyChunk();
			parentChunk.EnableChunk();
		}

		public void EnableChunk()
		{
			owner.activeChunks.Add(this);
			chunk.transform.gameObject.SetActive(value: true);
			foreach (Transform rock in rocks)
			{
				RockSelector.main.rockInstances.Add(rock);
			}
			SetLayer(owner.layer);
		}

		public void DisableChunk()
		{
			owner.activeChunks.Remove(this);
			chunk.transform.gameObject.SetActive(value: false);
			foreach (Transform rock in rocks)
			{
				RockSelector.main.rockInstances.Remove(rock);
			}
		}

		public void DestroyChunk()
		{
			owner.allChunks.Remove(this);
			owner.activeChunks.Remove(this);
			UnityEngine.Object.Destroy(chunk.transform.gameObject);
			UnityEngine.Object.DestroyImmediate(chunk.mesh);
			foreach (Transform rock in rocks)
			{
				RockSelector.main.rockInstances.Remove(rock);
			}
		}

		public void SetLayer(int layer)
		{
			chunk.transform.gameObject.layer = layer;
		}
	}

	public const int BaseChunkCount = 8;

	public Transform chunkPrefab;

	public TerrainModule.RockData rockData;

	public Transform rockPrefab;

	public Material material;

	public bool useTerrainUV;

	private float verticeSizeMultiplier = 1f;

	private float loadDistanceMultiplier = 1f;

	private Action<DynamicChunk> setupChunk;

	private Planet planet;

	private Double3 position;

	private double distanceMoved;

	private int layer;

	private List<DynamicChunk> allChunks = new List<DynamicChunk>();

	private List<DynamicChunk> activeChunks = new List<DynamicChunk>();

	private DynamicChunk bestSplit;

	private DynamicChunk bestMerge;

	public static DynamicTerrain Create(Planet planet, Double3 position, Action<DynamicChunk> setupChunk, float verticeSizeMultiplier, float loadDistanceMultiplier, string layer, Material material, bool useTerrainUV, Transform chunkPrefab, TerrainModule.RockData rockData, Transform rockPrefab)
	{
		DynamicTerrain dynamicTerrain = new GameObject(planet.codeName + " Dynamic Terrain").AddComponent<DynamicTerrain>();
		dynamicTerrain.setupChunk = setupChunk;
		dynamicTerrain.verticeSizeMultiplier = verticeSizeMultiplier;
		dynamicTerrain.loadDistanceMultiplier = loadDistanceMultiplier;
		dynamicTerrain.layer = LayerMask.NameToLayer(layer);
		dynamicTerrain.material = material;
		dynamicTerrain.useTerrainUV = useTerrainUV;
		dynamicTerrain.chunkPrefab = chunkPrefab;
		dynamicTerrain.rockData = rockData;
		dynamicTerrain.rockPrefab = rockPrefab;
		dynamicTerrain.Initialized(planet, position);
		return dynamicTerrain;
	}

	private void Initialized(Planet planet, Double3 position)
	{
		position.z *= 0.5;
		this.planet = planet;
		this.position = position;
		distanceMoved = 1.0;
		for (int i = 0; i < 8; i++)
		{
			new DynamicChunk(this, null, (double)i / 8.0, 0);
		}
		LoadFully();
	}

	public void LoadFully()
	{
		for (int i = 0; i < 500; i++)
		{
			DynamicChunk dynamicChunk = GetBestSplit();
			if (distanceMoved > dynamicChunk.updateSplit)
			{
				dynamicChunk.TrySplit(this);
				CalculateBest();
			}
		}
	}

	public void SetViewPosition(Double3 newPosition)
	{
		newPosition.z *= 0.5;
		distanceMoved += (position - newPosition).magnitude;
		position = newPosition;
	}

	public void SetLayer(string layer)
	{
		this.layer = LayerMask.NameToLayer(layer);
		foreach (DynamicChunk activeChunk in activeChunks)
		{
			activeChunk.SetLayer(this.layer);
		}
	}

	private void Update()
	{
		for (int i = 0; i < 20; i++)
		{
			DynamicChunk dynamicChunk = GetBestSplit();
			if (distanceMoved > dynamicChunk.updateSplit)
			{
				dynamicChunk.TrySplit(this);
				CalculateBest();
				continue;
			}
			DynamicChunk dynamicChunk2 = GetBestMerge();
			if (distanceMoved > dynamicChunk2.updateMerge)
			{
				dynamicChunk2.TryMerge(this);
				CalculateBest();
				continue;
			}
			break;
		}
	}

	private void OnDestroy()
	{
		DynamicChunk[] array = allChunks.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DestroyChunk();
		}
	}

	private DynamicChunk GetBestSplit()
	{
		if (bestSplit == null || bestSplit.chunk.transform == null)
		{
			CalculateBest();
		}
		return bestSplit;
	}

	private DynamicChunk GetBestMerge()
	{
		if (bestMerge == null || bestMerge.chunk.transform == null)
		{
			CalculateBest();
		}
		return bestMerge;
	}

	private void CalculateBest()
	{
		DynamicChunk dynamicChunk = activeChunks[0];
		foreach (DynamicChunk activeChunk in activeChunks)
		{
			if (activeChunk.updateSplit < dynamicChunk.updateSplit)
			{
				dynamicChunk = activeChunk;
			}
		}
		DynamicChunk dynamicChunk2 = activeChunks[0];
		foreach (DynamicChunk activeChunk2 in activeChunks)
		{
			if (activeChunk2.updateMerge < dynamicChunk2.updateMerge && activeChunk2.CanMerge())
			{
				dynamicChunk2 = activeChunk2;
			}
		}
		bestSplit = dynamicChunk;
		bestMerge = dynamicChunk2;
	}
}
