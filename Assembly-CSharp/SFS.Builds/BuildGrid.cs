using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class BuildGrid : MonoBehaviour
{
	public class PartCollider
	{
		public PolygonData module;

		public ConvexPolygon[] colliders;

		public void UpdateColliders()
		{
			colliders = module.polygon.GetConvexPolygonsWorld(module.transform);
		}
	}

	public PartGrid activeGrid;

	public PartGrid inactiveGrid;

	[Space]
	public GridSize gridSize;

	[Space]
	public BuildSelector selector;

	[Space]
	public Transform gridTransform;

	public List<PartCollider> buildColliders = new List<PartCollider>();

	public void Duplicate()
	{
		Part[] array = selector.selected.Where((Part x) => activeGrid.partsHolder.parts.Contains(x)).ToArray();
		if (array.Length != 0)
		{
			Part[] array2 = PartsLoader.DuplicateParts(BuildManager.main.holdGrid.holdGrid.selectedLayer, array);
			Part[] array3 = array2;
			for (int num = 0; num < array3.Length; num++)
			{
				array3[num].transform.position += new Vector3((BuildOrientation.main.rotation != 0f) ? (-0.5f) : 0.5f, 0.5f);
			}
			selector.DeselectAll();
			selector.Select(array2);
			BuildManager.main.holdGrid.StartImportOrDuplicate(array2, forDuplicate: true, applyUndo: true, null);
		}
	}

	public void ApplyOrientationChange_BuildGrid(Orientation change, Vector2 round)
	{
		Undo.main.CreateNewStep("orientation change");
		Part[] selectedParts = GetSelectedParts();
		Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, selectedParts);
		Vector2 pivot = bounds.center.Round(round);
		if (BuildManager.main.symmetryMode)
		{
			Vector2 pivot2 = new Vector2(gridSize.centerX * 2f - pivot.x, pivot.y);
			Orientation change2 = new Orientation(change.x, change.y, 0f - change.z);
			ApplyOrientationChange_BuildGrid(change2, pivot2, GetMirrorParts(selectedParts));
		}
		ApplyOrientationChange_BuildGrid(change, pivot, selectedParts);
		activeGrid.UpdateAdaptation();
	}

	private void ApplyOrientationChange_BuildGrid(Orientation change, Vector2 pivot, Part[] parts)
	{
		List<Part> intersecting = CollectIntersecting(parts);
		List<int> list = new List<int>();
		List<Part> list2 = new List<Part>();
		Part[] array = parts;
		foreach (Part item in array)
		{
			int num = activeGrid.partsHolder.parts.IndexOf(item);
			if (num != -1)
			{
				list.Add(num);
				list2.Add(item);
			}
		}
		Undo.main.RecordPartAction(new Undo.AddPart(list2.ToArray(), list, Undo.GridType.ActiveGrid, add: false));
		List<int> list3 = new List<int>();
		List<Part> list4 = new List<Part>();
		array = parts;
		foreach (Part item2 in array)
		{
			int num2 = inactiveGrid.partsHolder.parts.IndexOf(item2);
			if (num2 != -1)
			{
				list3.Add(num2);
				list4.Add(item2);
			}
		}
		Undo.main.RecordPartAction(new Undo.AddPart(list4.ToArray(), list3, Undo.GridType.InactiveGrid, add: false));
		Part_Utility.ApplyOrientationChange(change, pivot, parts);
		Undo.main.RecordPartAction(new Undo.AddPart(list2.ToArray(), list, Undo.GridType.ActiveGrid, add: true));
		Undo.main.RecordPartAction(new Undo.AddPart(list4.ToArray(), list3, Undo.GridType.InactiveGrid, add: true));
		DisableIntersecting(!SandboxSettings.main.settings.partClipping, applyUndo: true, parts);
		TryEnableNonIntersecting(intersecting, applyUndo: true);
	}

	public Part[] GetSelectedParts()
	{
		return selector.selected.Where((Part x) => activeGrid.partsHolder.parts.Contains(x)).ToArray();
	}

	public Part[] GetMirrorParts(params Part[] parts)
	{
		List<Part> list = new List<Part>();
		foreach (Part part in parts)
		{
			list.AddRange(activeGrid.partsHolder.parts.Where((Part mirrorPart) => IsMirror(part, mirrorPart)));
			list.AddRange(inactiveGrid.partsHolder.parts.Where((Part mirrorPart) => IsMirror(part, mirrorPart)));
		}
		return list.ToArray();
		bool IsMirror(Part A, Part B)
		{
			if (Mathf.Abs(B.transform.position.y - A.transform.position.y) > 0.01f || Mathf.Abs(B.transform.position.x - (gridSize.centerX * 2f - A.transform.position.x)) > 0.01f)
			{
				return false;
			}
			if (((Vector2)B.transform.TransformVector(Vector2.one) - A.transform.TransformVector(Vector2.one) * new Vector2(-1f, 1f)).sqrMagnitude > 0.01f)
			{
				return false;
			}
			if (B.name != A.name)
			{
				return false;
			}
			return true;
		}
	}

	public bool RaycastParts(Vector2 worldPoint, out PartHit hit)
	{
		if (Part_Utility.RaycastParts(activeGrid.partsHolder.GetArray(), worldPoint, 0.3f, out hit))
		{
			return true;
		}
		if (Part_Utility.RaycastParts(inactiveGrid.partsHolder.GetArray(), worldPoint, 0.3f, out hit))
		{
			return true;
		}
		hit = null;
		return false;
	}

	private void MoveParts(bool active, bool enableNonIntersecting, bool applyUndo, params Part[] parts)
	{
		RemoveParts(enableNonIntersecting, applyUndo, parts);
		AddParts(active, clamp: false, applyUndo, parts);
	}

	public void AddParts(bool active, bool clamp, bool applyUndo, params Part[] parts)
	{
		if (clamp)
		{
			ClampToLimits(parts);
		}
		Part[] array = (active ? parts.Where((Part a) => a.GetOwnershipState() == OwnershipState.OwnedAndUnlocked).ToArray() : new Part[0]);
		Part[] array2 = ((!active) ? parts : parts.Where((Part a) => a.GetOwnershipState() != OwnershipState.OwnedAndUnlocked).ToArray());
		if (array.Length != 0)
		{
			Part[] array3 = array.ToArray();
			int startIndex = activeGrid.partsHolder.parts.Count;
			activeGrid.AddParts(array3);
			if (applyUndo)
			{
				Undo.main.RecordPartAction(new Undo.AddPart(array3, array3.Select((Part _, int i) => startIndex + i).ToList(), Undo.GridType.ActiveGrid, add: true));
			}
			DisableIntersecting(!SandboxSettings.main.settings.partClipping, applyUndo, array3);
		}
		if (array2.Length == 0)
		{
			return;
		}
		Part[] array4 = array2.ToArray();
		int startIndex2 = inactiveGrid.partsHolder.parts.Count;
		inactiveGrid.AddParts(array4);
		if (applyUndo)
		{
			Undo.main.RecordPartAction(new Undo.AddPart(array4, array4.Select((Part _, int i) => startIndex2 + i).ToList(), Undo.GridType.InactiveGrid, add: true));
		}
	}

	public void RemoveParts(bool enableNonIntersecting, bool applyUndo, params Part[] parts)
	{
		if (applyUndo)
		{
			Undo.main.RecordRemoveParts(parts, add: false, alwaysSave: false);
		}
		activeGrid.RemoveParts(parts);
		inactiveGrid.RemoveParts(parts);
		if (enableNonIntersecting)
		{
			EnableNonIntersecting(parts, applyUndo);
		}
	}

	private void DisableIntersecting(bool checkAddedPartsForCollisions, bool applyUndo, params Part[] addedParts)
	{
		if (!checkAddedPartsForCollisions || activeGrid.partsHolder.parts.Count == addedParts.Length)
		{
			return;
		}
		(Part, (ConvexPolygon[], bool))[] buildColliderPolygons_WithPart = Part_Utility.GetBuildColliderPolygons_WithPart(addedParts);
		List<Part> list = new List<Part>();
		foreach (Part activePart in activeGrid.partsHolder.parts)
		{
			if (!addedParts.Contains(activePart))
			{
				ConvexPolygon[] item = activePart.GetBuildColliderPolygons().Item1;
				ConvexPolygon[] b = buildColliderPolygons_WithPart.Where(((Part part, (ConvexPolygon[] polyogns, bool isFront)) c) => c.part != activePart && c.part.IsFront() == activePart.IsFront()).SelectMany(((Part part, (ConvexPolygon[] polyogns, bool isFront)) c) => c.Item2.polyogns).ToArray();
				if (Polygon.Intersect(item, b, -0.08f))
				{
					list.Add(activePart);
				}
			}
		}
		if (list.Count > 0)
		{
			MoveParts(active: false, enableNonIntersecting: false, applyUndo, list.ToArray());
		}
	}

	private void EnableNonIntersecting(Part[] removedParts, bool applyUndo)
	{
		TryEnableNonIntersecting(CollectIntersecting(removedParts), applyUndo);
	}

	private List<Part> CollectIntersecting(Part[] removedParts)
	{
		(ConvexPolygon[], ConvexPolygon[]) buildColliderPolygons = Part_Utility.GetBuildColliderPolygons(removedParts);
		List<Part> list = new List<Part>();
		foreach (Part part in inactiveGrid.partsHolder.parts)
		{
			(ConvexPolygon[], bool isFront) buildColliderPolygons2 = part.GetBuildColliderPolygons();
			ConvexPolygon[] item = buildColliderPolygons2.Item1;
			ConvexPolygon[] b;
			if (!buildColliderPolygons2.isFront)
			{
				(b, _) = buildColliderPolygons;
			}
			else
			{
				b = buildColliderPolygons.Item2;
			}
			if (Polygon.Intersect(item, b, -0.08f))
			{
				list.Add(part);
			}
		}
		return list;
	}

	private void TryEnableNonIntersecting(List<Part> intersecting, bool applyUndo)
	{
		(ConvexPolygon[], ConvexPolygon[]) buildColliderPolygons = Part_Utility.GetBuildColliderPolygons(activeGrid.partsHolder.GetArray());
		List<Part> list = new List<Part>();
		foreach (Part inactivePart in intersecting)
		{
			(ConvexPolygon[], bool) buildColliderPolygons2 = inactivePart.GetBuildColliderPolygons();
			var (a, _) = buildColliderPolygons2;
			ConvexPolygon[] b;
			if (!buildColliderPolygons2.Item2)
			{
				(b, _) = buildColliderPolygons;
			}
			else
			{
				b = buildColliderPolygons.Item2;
			}
			if (!Polygon.Intersect(a, b, -0.08f) && list.All((Part part_B) => !Part_Utility.CollidersIntersect(inactivePart, part_B)))
			{
				list.Add(inactivePart);
			}
		}
		if (list.Count > 0)
		{
			MoveParts(active: true, enableNonIntersecting: false, applyUndo, list.ToArray());
		}
	}

	private void ClampToLimits(params Part[] parts)
	{
		if (!SandboxSettings.main.settings.infiniteBuildArea && Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, parts))
		{
			Math_Utility.GetRectOverreach(bounds, new Rect(Vector2.zero, gridSize.size.Value), clampNegative: true, out var leftDown, out var rightUp);
			Vector2 vector = (leftDown - rightUp).Round(0.5f);
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i].Position += vector;
			}
		}
	}

	private void Start()
	{
		OnPartsAdded(activeGrid.partsHolder.GetArray());
		PartHolder partsHolder = activeGrid.partsHolder;
		partsHolder.onPartsAdded = (Action<Part[]>)Delegate.Combine(partsHolder.onPartsAdded, new Action<Part[]>(OnPartsAdded));
		PartHolder partsHolder2 = activeGrid.partsHolder;
		partsHolder2.onPartsRemoved = (Action<Part[]>)Delegate.Combine(partsHolder2.onPartsRemoved, (Action<Part[]>)delegate(Part[] parts)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				PolygonData[] modules = parts[i].GetModules<PolygonData>();
				foreach (PolygonData polygonData in modules)
				{
					if (polygonData.BuildCollider_IncludeInactive)
					{
						for (int num = buildColliders.Count - 1; num >= 0; num--)
						{
							if (buildColliders[num].module == polygonData)
							{
								buildColliders[num].module.onChange -= new Action(buildColliders[num].UpdateColliders);
								buildColliders.RemoveAt(num);
								break;
							}
						}
					}
				}
			}
		});
		void OnPartsAdded(Part[] parts)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				PolygonData[] modules = parts[i].GetModules<PolygonData>();
				foreach (PolygonData polygonData in modules)
				{
					if (polygonData.BuildCollider_IncludeInactive)
					{
						PartCollider partCollider = new PartCollider
						{
							module = polygonData,
							colliders = null
						};
						polygonData.onChange += new Action(partCollider.UpdateColliders);
						buildColliders.Add(partCollider);
					}
				}
			}
		}
	}
}
