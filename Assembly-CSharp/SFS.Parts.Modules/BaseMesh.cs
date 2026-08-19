using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SFS.Parts.Modules;

[ExecuteInEditMode]
public abstract class BaseMesh : MonoBehaviour
{
	[FormerlySerializedAs("sortingLayerOffset")]
	public int renderQueueOffset;

	private static int baseDepthID = Shader.PropertyToID("_BaseDepth");

	private static int depthMultiplierID = Shader.PropertyToID("_DepthMultiplier");

	private string sortingLayer;

	private Mesh meshReference;

	public abstract void GenerateMesh();

	protected void ApplyMeshData(List<Vector3> vertices, int[] indices, List<Vector3>[] UVs, Color[] colors, List<Vector3> shading, List<Vector3> depths, float baseDepth, float depthMultiplier, List<PartTex> textures, MeshTopology topology)
	{
		Mesh mesh = GetMesh();
		Renderer meshRenderer = this.GetOrAddComponent<MeshRenderer>();
		mesh.SetVertices(vertices);
		for (int i = 0; i < UVs.Length; i++)
		{
			mesh.SetUVs(i, UVs[i]);
		}
		mesh.colors = colors;
		mesh.SetUVs(5, shading);
		if (RenderSortingManager.main != null)
		{
			baseDepth = RenderSortingManager.main.GetGlobalDepth(0.5f, sortingLayer) + baseDepth * 0.02f * 1f / (float)Mathf.Max(RenderSortingManager.main.layers.Count, 1);
			depthMultiplier = depthMultiplier * 0.02f * 1f / (float)Mathf.Max(RenderSortingManager.main.layers.Count, 1);
		}
		else
		{
			baseDepth = 0.5f + baseDepth * 0.02f;
			depthMultiplier *= 0.02f;
		}
		mesh.SetUVs(3, depths);
		if (textures.Count > 1)
		{
			Set_TrySubmeshes();
		}
		else
		{
			Set_Basic();
		}
		mesh.RecalculateBounds();
		void Set_Basic()
		{
			mesh.SetIndices(indices, topology, 0);
			SetMaterials(1);
			if (textures.Count > 0)
			{
				SetPropertyBlock(textures[0], 0);
			}
		}
		void Set_Multiple(Dictionary<PartTex, List<int>> submeshes)
		{
			SetMaterials(submeshes.Count);
			mesh.subMeshCount = submeshes.Count;
			int num = 0;
			foreach (List<int> value in submeshes.Values)
			{
				List<int> list = new List<int>(value.Count * 4);
				foreach (int item in value)
				{
					list.Add(indices[item * 4]);
					list.Add(indices[item * 4 + 1]);
					list.Add(indices[item * 4 + 2]);
					list.Add(indices[item * 4 + 3]);
				}
				SetPropertyBlock(textures[value[0]], num);
				mesh.SetIndices(list, topology, num);
				num++;
			}
		}
		void Set_TrySubmeshes()
		{
			Dictionary<PartTex, List<int>> dictionary = new Dictionary<PartTex, List<int>>();
			for (int j = 0; j < textures.Count; j++)
			{
				PartTex key = textures[j];
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, new List<int>());
				}
				dictionary[key].Add(j);
			}
			if (dictionary.Count > 1)
			{
				Set_Multiple(dictionary);
			}
			else
			{
				Set_Basic();
			}
		}
		void SetMaterials(int count)
		{
			Material material = null;
			if (Application.isPlaying)
			{
				int renderQueue = RenderSortingManager.main.GetRenderQueue(sortingLayer) + renderQueueOffset;
				material = RenderSortingManager.main.GetPartMaterial(renderQueue);
			}
			meshRenderer.sharedMaterials = new Material[count].Select((Material a) => material).ToArray();
		}
		void SetPropertyBlock(PartTex T, int index)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			if (meshRenderer.HasPropertyBlock())
			{
				meshRenderer.GetPropertyBlock(materialPropertyBlock, index);
			}
			materialPropertyBlock.SetTexture(PartTex.ColorTexture, T.color);
			materialPropertyBlock.SetTexture(PartTex.ShapeTexture, T.shape);
			materialPropertyBlock.SetTexture(PartTex.ShadowTexture, T.shadow);
			materialPropertyBlock.SetFloat(baseDepthID, baseDepth);
			materialPropertyBlock.SetFloat(depthMultiplierID, depthMultiplier);
			meshRenderer.SetPropertyBlock(materialPropertyBlock, index);
		}
	}

	private Mesh GetMesh()
	{
		MeshFilter orAddComponent = this.GetOrAddComponent<MeshFilter>();
		if (orAddComponent.sharedMesh == null || !Application.isPlaying)
		{
			Mesh obj = new Mesh
			{
				name = "BaseMesh"
			};
			Mesh sharedMesh = obj;
			meshReference = obj;
			orAddComponent.sharedMesh = sharedMesh;
		}
		else
		{
			orAddComponent.sharedMesh.Clear();
		}
		return orAddComponent.sharedMesh;
	}

	public void SetSortingLayer(string sortingLayer)
	{
		if (!(this.sortingLayer == sortingLayer))
		{
			this.sortingLayer = sortingLayer;
			GenerateMesh();
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(meshReference);
	}
}
