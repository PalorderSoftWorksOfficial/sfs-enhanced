using System.Collections.Generic;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class PartGrid : MonoBehaviour
{
	public PartHolder partsHolder;

	public string selectedLayer;

	public RenderSortingManager renderSortingManager;

	private List<string> GetLayers()
	{
		if (!(renderSortingManager != null))
		{
			return new List<string>();
		}
		return renderSortingManager.layers;
	}

	public void AddParts(params Part[] parts)
	{
		foreach (Part obj in parts)
		{
			obj.transform.parent = base.transform;
			obj.SetSortingLayer(selectedLayer);
		}
		partsHolder.AddParts(parts);
		UpdateAdaptation();
	}

	public void AddPartAtIndex(int index, Part part)
	{
		part.transform.parent = base.transform;
		part.SetSortingLayer(selectedLayer);
		partsHolder.AddPartAtIndex(index, part);
		UpdateAdaptation();
	}

	public void RemoveParts(params Part[] parts)
	{
		partsHolder.RemoveParts(parts);
		UpdateAdaptation();
	}

	public void RemovePartAtIndex(int index)
	{
		partsHolder.RemovePartAtIndex(index);
		UpdateAdaptation();
	}

	public void UpdateAdaptation()
	{
		Part[] array = partsHolder.GetArray();
		AdaptModule.UpdateAdaptation(array);
		MagnetModule.UpdateOccupied(array);
	}

	public void DestroyParts()
	{
		foreach (Part part in partsHolder.parts)
		{
			part.DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
		}
		partsHolder.ClearParts();
	}
}
