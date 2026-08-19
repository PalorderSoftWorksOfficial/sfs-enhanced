using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SFS.Parts.Modules;

public class InteriorModule : MonoBehaviour
{
	public enum LayerType
	{
		Interior,
		Exterior
	}

	[FormerlySerializedAs("location")]
	public LayerType layerType;

	private void Awake()
	{
		InteriorManager.main.interiorView.OnChange += new Action(UpdateActive);
	}

	private void OnDestroy()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			InteriorManager.main.interiorView.OnChange -= new Action(UpdateActive);
		}
	}

	private void UpdateActive()
	{
		bool value = InteriorManager.main.interiorView.Value;
		switch (layerType)
		{
		case LayerType.Interior:
			base.gameObject.SetActive(value);
			break;
		case LayerType.Exterior:
			base.gameObject.SetActive(!value);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
