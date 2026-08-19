using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SFS;

[ExecuteInEditMode]
public class RenderSortingModule : MonoBehaviour
{
	public enum SortingMode
	{
		DepthOnly,
		AbsoluteDepth_AndQueue,
		RelativeDepth_AndQueue,
		QueueOnly
	}

	private static readonly int Depth = Shader.PropertyToID("_Depth");

	public SortingMode sortingMode;

	[Range(0f, 1f)]
	public float depth;

	public string selectedLayer;

	public RenderSortingManager manager;

	[FormerlySerializedAs("layer")]
	public int renderQueue;

	public bool setSharedMaterial;

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
		switch (sortingMode)
		{
		case SortingMode.DepthOnly:
			SetDepth(base.gameObject, depth);
			break;
		case SortingMode.AbsoluteDepth_AndQueue:
			SetRenderQueue(base.gameObject, renderQueue);
			SetDepth(base.gameObject, depth);
			break;
		case SortingMode.RelativeDepth_AndQueue:
			SetRenderQueue(base.gameObject, manager.GetRenderQueue(selectedLayer));
			SetDepth(base.gameObject, manager.GetGlobalDepth(depth, selectedLayer));
			break;
		case SortingMode.QueueOnly:
			SetRenderQueue(base.gameObject, renderQueue);
			break;
		}
	}

	private void SetDepth(GameObject gameObject, float depth)
	{
		Renderer component = gameObject.GetComponent<Renderer>();
		((setSharedMaterial || !Application.isPlaying) ? component.sharedMaterial : component.material).SetFloat(Depth, depth);
	}

	private void SetRenderQueue(GameObject gameObject, int renderQueue)
	{
		Renderer component = gameObject.GetComponent<Renderer>();
		((setSharedMaterial || !Application.isPlaying) ? component.sharedMaterial : component.material).renderQueue = renderQueue;
	}
}
