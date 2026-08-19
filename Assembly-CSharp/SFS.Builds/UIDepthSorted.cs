using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Builds;

public class UIDepthSorted : MonoBehaviour
{
	public string selectedLayer;

	public RenderSortingManager manager;

	public Shader shader;

	private List<string> GetLayers()
	{
		if (!(manager != null))
		{
			return new List<string>();
		}
		return manager.layers;
	}

	private void Start()
	{
		Graphic component = GetComponent<Graphic>();
		Material material = new Material(shader);
		float globalDepth = manager.GetGlobalDepth(1f, selectedLayer);
		material.SetFloat("_Depth", globalDepth);
		material.renderQueue = manager.GetRenderQueue(selectedLayer);
		component.material = material;
	}

	private bool Validate()
	{
		if (!(GetComponent<Image>() != null))
		{
			return !(GetComponent<RawImage>() != null);
		}
		return false;
	}
}
