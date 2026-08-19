using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.Builds;

public class BuildState : MonoBehaviour
{
	public static BuildState main;

	public BuildCamera buildCamera;

	public BuildMenus buildMenus;

	public BuildGrid buildGrid;

	public BuildSelector selector;

	private void Awake()
	{
		main = this;
	}

	public void UpdatePersistent_NoCache()
	{
		SavingCache.main.SaveBuildPersistent(GetBlueprint(), cache: false);
	}

	public void UpdatePersistent(bool forceVertical = false)
	{
		SavingCache.main.SaveBuildPersistent(GetBlueprint(forceVertical), cache: true);
	}

	public Blueprint GetBlueprint(bool forceVertical = false)
	{
		List<Part> parts = buildGrid.activeGrid.partsHolder.parts;
		return new Blueprint(PartSave.CreateSaves(parts.ToArray()), StageSave.CreateSaves(buildMenus.staging, parts), buildGrid.gridSize.centerX, forceVertical ? 0f : BuildOrientation.main.rotation, InteriorManager.main.interiorView);
	}

	public void LoadPersistent()
	{
		CenterCameraOnParts();
		if (SavingCache.main.TryLoadBuildPersistent(MsgDrawer.main, out var buildPersistent, eraseCache: true))
		{
			LoadBlueprint(buildPersistent, Menu.read, autoCenterParts: false, applyUndo: false);
		}
	}

	public void LoadBlueprint(Blueprint blueprint, I_MsgLogger logger, bool autoCenterParts, bool applyUndo, Vector2 offset = default(Vector2), Action onLoaded = null)
	{
		Clear(applyUndo);
		if (blueprint.offset != Vector2.zero)
		{
			PartSave[] parts = blueprint.parts;
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i].position += blueprint.offset;
			}
		}
		Part[] parts2 = SpawnBlueprint(blueprint, applyUndo, logger).ToArray();
		if (autoCenterParts)
		{
			Part_Utility.CenterParts(parts2, GridSize.GetOwnedGridSize(returnZeroIfInfinite: true));
		}
		else
		{
			float centerX = buildGrid.gridSize.centerX;
			float num = ((!float.IsNaN(blueprint.center)) ? blueprint.center : centerX);
			offset.x += centerX - num;
			Part_Utility.OffsetPartPosition(offset, round: false, parts2);
		}
		CenterCameraOnParts(parts2);
		BuildOrientation.main.SetOrientation(blueprint.rotation, animate: false);
		buildMenus.staging.Load(blueprint.stages, parts2, applyUndo);
		onLoaded?.Invoke();
	}

	private Part[] SpawnBlueprint(Blueprint blueprint, bool applyUndo, I_MsgLogger logger)
	{
		Part[] array = PartsLoader.CreateParts(blueprint.parts, buildGrid.activeGrid.transform, buildGrid.activeGrid.selectedLayer, OnPartNotOwned.Allow, out var ownershipState);
		buildGrid.AddParts(active: true, clamp: false, applyUndo, array.Where((Part p) => p.GetOwnershipState() == OwnershipState.OwnedAndUnlocked).ToArray());
		buildGrid.AddParts(active: false, clamp: false, applyUndo, array.Where((Part p) => p.GetOwnershipState() != OwnershipState.OwnedAndUnlocked).ToArray());
		if (ownershipState.Any((OwnershipState a) => a == OwnershipState.NotOwned))
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => "Not all parts are owned\nDisabled not owned parts\n\nView full version?", () => "Full Version", BuildManager.main.OpenFullVersionSalePage);
		}
		else if (ownershipState.Any((OwnershipState a) => a == OwnershipState.NotUnlocked))
		{
			logger.Log("Not all parts are unlocked\n\nDisabled not owned parts");
		}
		InteriorManager.main.interiorView.Value = blueprint.interiorView;
		return array;
	}

	public void CenterCameraOnParts(params Part[] parts)
	{
		bool buildColliderBounds_WorldSpace = Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, parts);
		buildCamera.CameraPosition = (buildColliderBounds_WorldSpace ? bounds.center : (buildGrid.gridSize.size.Value * new Vector2(0.5f, 0.65f)));
		buildCamera.CameraDistance = (buildColliderBounds_WorldSpace ? (bounds.height * 1.8f) : 20f);
	}

	public void Clear(bool applyUndo)
	{
		ClearCrew(buildGrid.activeGrid.partsHolder);
		ClearCrew(buildGrid.inactiveGrid.partsHolder);
		if (applyUndo)
		{
			Undo.main.CreateNewStep("Clear");
		}
		buildMenus.staging.ClearStages(applyUndo);
		selector.DeselectAll();
		if (applyUndo)
		{
			Record(buildGrid.activeGrid, active: true);
			Record(buildGrid.inactiveGrid, active: true);
		}
		buildGrid.activeGrid.DestroyParts();
		buildGrid.inactiveGrid.DestroyParts();
		static void ClearCrew(PartHolder a)
		{
			CrewModule[] modules = a.GetModules<CrewModule>();
			for (int i = 0; i < modules.Length; i++)
			{
				CrewModule.Seat[] seats = modules[i].seats;
				for (int j = 0; j < seats.Length; j++)
				{
					seats[j].Exit();
				}
			}
		}
		static void Record(PartGrid grid, bool active)
		{
			Part[] array = grid.partsHolder.GetArray();
			Undo.main.RecordPartAction(new Undo.AddPart(array, array.Select((Part _, int i) => i).ToList(), (!active) ? Undo.GridType.InactiveGrid : Undo.GridType.ActiveGrid, add: false));
		}
	}
}
