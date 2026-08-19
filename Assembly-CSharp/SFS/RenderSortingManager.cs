using System.Collections.Generic;
using UnityEngine;

namespace SFS;

public class RenderSortingManager : MonoBehaviour
{
	public static RenderSortingManager main;

	public List<string> layers = new List<string>();

	public Dictionary<(Material, int), Material> partMaterials = new Dictionary<(Material, int), Material>();

	private const int Transparent_Queue = 3000;

	private void Awake()
	{
		main = this;
	}

	public Material GetPartMaterial(int renderQueue)
	{
		return GetMaterial(renderQueue, Base.partsLoader.partMaterial);
	}

	public Material GetPartModelMaterial(int renderQueue, bool normals)
	{
		return GetMaterial(renderQueue, normals ? Base.partsLoader.partModelMaterialNormals : Base.partsLoader.partModelMaterialTexture);
	}

	private Material GetMaterial(int renderQueue, Material prefab)
	{
		(Material, int) key = (prefab, renderQueue);
		if (!partMaterials.ContainsKey(key))
		{
			Material material = Object.Instantiate(prefab);
			material.name = "PartModel " + (renderQueue - 3000) + " " + material.GetInstanceID();
			material.renderQueue = renderQueue;
			partMaterials.Add(key, material);
		}
		return partMaterials[key];
	}

	public float GetGlobalDepth(float localDepth, string layer)
	{
		if (!layers.Contains(layer))
		{
			return localDepth;
		}
		int layerIndex = GetLayerIndex(layer);
		float num = 1f / (float)layers.Count;
		float num2 = (float)layerIndex * num;
		return Mathf.Lerp(num2, num2 + num, localDepth);
	}

	public int GetRenderQueue(string layer)
	{
		if (!layers.Contains(layer))
		{
			return 3000;
		}
		return 3000 + GetLayerIndex(layer) * 50;
	}

	private int GetLayerIndex(string layer)
	{
		return layers.Count - 1 - layers.IndexOf(layer);
	}
}
