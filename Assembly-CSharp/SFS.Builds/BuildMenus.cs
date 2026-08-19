using System;
using System.Linq;
using SFS.Audio;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class BuildMenus : MonoBehaviour
{
	[Space]
	public GameObject pickHolder_UICamera;

	public GameObject pickHolder_UI;

	[Space]
	public StagingDrawer stagingDrawer;

	public Staging staging;

	[Space]
	public BuildSelector selector;

	public GameObject selectMenu;

	public GameObject unselectMenu;

	public SkinMenu colorSkins;

	public SkinMenu shapeSkins;

	[Space]
	public AttachableStatsMenu partMenu;

	public RectTransform partStatsPosition;

	[Space]
	public BuildStatsDrawer statsDrawer;

	[Space]
	public RectTransform symmetryDisabled;

	public RectTransform symmetryEnabled;

	public ButtonPC symmetryButtonPc;

	[Space]
	public Bool_Local showBuildMenus;

	public Bool_Local stagingMode;

	public OpenTracker attachedPartMenuState = new OpenTracker();

	private float lastClickTime;

	private Part lastClickedPart;

	private PartSave[] clipboard;

	private float offset;

	public bool IsEditingStage
	{
		get
		{
			if ((bool)stagingMode)
			{
				return stagingDrawer.HasStageSelected();
			}
			return false;
		}
	}

	private void Start()
	{
		showBuildMenus.OnChange += new Action(UpdateLeftUI);
		stagingMode.OnChange += new Action(UpdateLeftUI);
		BuildSelector buildSelector = BuildManager.main.selector;
		buildSelector.onSelectedChange = (Action)Delegate.Combine(buildSelector.onSelectedChange, new Action(UpdateSelectUI));
		PartHolder partsHolder = BuildManager.main.holdGrid.holdGrid.partsHolder;
		partsHolder.onPartsChanged = (Action)Delegate.Combine(partsHolder.onPartsChanged, new Action(UpdateSelectUI));
		BuildSelector buildSelector2 = BuildManager.main.selector;
		buildSelector2.onSelectedChange = (Action)Delegate.Combine(buildSelector2.onSelectedChange, (Action)delegate
		{
			if (attachedPartMenuState.isOpen)
			{
				attachedPartMenuState.Close();
				if (BuildManager.main.selector.selected.Count > 0)
				{
					Part[] allParts = BuildManager.main.selector.selected.ToArray();
					attachedPartMenuState = partMenu.Open_DrawPart(null, allParts, PartDrawSettings.BuildSettings, () => partStatsPosition.TransformPoint(Vector3.zero), dontUpdateOnZoomChange: false, skipAnimation: true);
				}
			}
		});
		stagingDrawer.Initialize_Build();
		BuildManager.main.pickGrid.OnPartMenuOpen += delegate
		{
			BuildManager.main.pickGrid.categoriesMenu.expandMenu.expanded.Value = false;
		};
		BuildManager.main.pickGrid.categoriesMenu.onMenuOpen += delegate
		{
			BuildManager.main.pickGrid.partMenuState.Close();
		};
		KeysNode keysNode = BuildManager.main.build_Input.keysNode;
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Close_Menu, BuildManager.main.OpenMenu);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.SaveLoad[0], BuildManager.main.OpenSaveMenu);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.SaveLoad[1], BuildManager.main.OpenLoadMenu);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Rotate_Part[0], delegate
		{
			Rotate(90f);
		});
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Rotate_Part[1], delegate
		{
			Rotate(-90f);
		});
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Flip_Part[1], FlipX);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Flip_Part[3], FlipX);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Flip_Part[0], FlipY);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Flip_Part[2], FlipY);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.CopyPaste[0], Copy);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.CopyPaste[1], Paste);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Select_All, SelectAll);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Duplicate, BuildManager.main.buildGrid.Duplicate);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Delete, Delete);
		colorSkins.DrawSkinButtons();
		shapeSkins.DrawSkinButtons();
		BuildSelector buildSelector3 = BuildManager.main.selector;
		buildSelector3.onSelectedChange = (Action)Delegate.Combine(buildSelector3.onSelectedChange, (Action)delegate
		{
			colorSkins.DrawSkinButtons();
			shapeSkins.DrawSkinButtons();
		});
		static void Delete()
		{
			if (BuildManager.main.holdGrid.HasParts(includePreHeld: false))
			{
				BuildManager.main.holdGrid.EndHold(destroy: true);
			}
			else
			{
				Undo.main.CreateNewStep("Delete");
				Part[] selectedParts = BuildManager.main.buildGrid.GetSelectedParts();
				Part[] array = selectedParts;
				foreach (Part part in array)
				{
					part.aboutToDestroy?.Invoke(part);
				}
				BuildManager.main.buildGrid.RemoveParts(enableNonIntersecting: true, applyUndo: true, selectedParts);
				BuildManager.main.selector.Deselect(selectedParts);
				array = selectedParts;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
				}
			}
		}
		static void SelectAll()
		{
			BuildManager.main.selector.Select(BuildManager.main.buildGrid.activeGrid.partsHolder.GetArray());
		}
	}

	private void OnDestroy()
	{
		BuildSelector buildSelector = BuildManager.main.selector;
		buildSelector.onSelectedChange = (Action)Delegate.Remove(buildSelector.onSelectedChange, new Action(UpdateSelectUI));
		PartHolder partsHolder = BuildManager.main.holdGrid.holdGrid.partsHolder;
		partsHolder.onPartsChanged = (Action)Delegate.Remove(partsHolder.onPartsChanged, new Action(UpdateSelectUI));
	}

	private void UpdateLeftUI()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			bool flag = showBuildMenus.Value && !stagingMode.Value;
			stagingMode.Value = true;
			showBuildMenus.Value = true;
			flag = true;
			pickHolder_UICamera.SetActive(flag);
			pickHolder_UI.SetActive(flag);
			bool value = showBuildMenus.Value && stagingMode.Value;
			stagingDrawer.shown.Value = value;
		}
	}

	public void UpdateSelectUI()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			bool active = BuildManager.main.selector.selected.Count > 0 && !BuildManager.main.holdGrid.HasParts(includePreHeld: false);
			selectMenu.SetActive(active);
		}
	}

	public void OpenAttachedPartsMenu()
	{
		Part[] array = BuildManager.main.selector.selected.ToArray();
		if (array.Length != 0 && !attachedPartMenuState.isOpen)
		{
			attachedPartMenuState = partMenu.Open_DrawPart(null, array, PartDrawSettings.BuildSettings, () => partStatsPosition.TransformPoint(Vector3.zero), dontUpdateOnZoomChange: false, skipAnimation: false);
		}
		else
		{
			attachedPartMenuState.Close();
		}
	}

	public void Rotate(float rotation)
	{
		bool num = BuildManager.main.holdGrid.HasParts(includePreHeld: false);
		Part[] parts = (num ? null : BuildManager.main.buildGrid.GetSelectedParts());
		if (!num && !SandboxSettings.main.settings.infiniteBuildArea && Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, parts) && bounds.height > GridSize.GetOwnedGridSize(returnZeroIfInfinite: false).x)
		{
			SoundPlayer.main.denySound.Play();
		}
		else
		{
			ApplyOrientationChange(new Orientation(1f, 1f, rotation), new Vector2(0.5f, 0.5f));
		}
	}

	public void FlipX()
	{
		bool flag = BuildOrientation.main.rotation != 0f;
		ApplyOrientationChange(new Orientation(flag ? 1 : (-1), (!flag) ? 1 : (-1), 0f), new Vector2(0.25f, 0.25f));
	}

	public void FlipY()
	{
		bool flag = BuildOrientation.main.rotation != 0f;
		ApplyOrientationChange(new Orientation((!flag) ? 1 : (-1), flag ? 1 : (-1), 0f), new Vector2(0.25f, 0.25f));
	}

	private static void ApplyOrientationChange(Orientation change, Vector2 round)
	{
		if (BuildManager.main.holdGrid.HasParts(includePreHeld: false))
		{
			Part[] array = BuildManager.main.holdGrid.holdGrid.partsHolder.GetArray();
			Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, array);
			Vector2 pivot = BuildManager.main.holdGrid.holdGrid.partsHolder.transform.InverseTransformPoint(bounds.center.Round(round));
			Part_Utility.ApplyOrientationChange(change, pivot, array);
		}
		else
		{
			BuildManager.main.buildGrid.ApplyOrientationChange_BuildGrid(change, round);
		}
	}

	public void OnEmptyClick()
	{
		selector.DeselectAll();
		stagingDrawer.SetSelected(null);
		partMenu.Close();
		BuildManager.main.pickGrid.categoriesMenu.expandMenu.Close();
	}

	public void OnPartClick(PartHit hit)
	{
		if (IsEditingStage)
		{
			Undo.main.CreateNewStep("Toggle Stage Part");
			Part part = hit.part;
			stagingDrawer.TogglePartSelected(part, playSound: true, createNewStep: false);
			if (!BuildManager.main.symmetryMode)
			{
				return;
			}
			Part[] mirrorParts = BuildManager.main.buildGrid.GetMirrorParts(hit.part);
			if (mirrorParts.Length == 1)
			{
				Part part2 = mirrorParts[0];
				if (stagingDrawer.IsPartSelected(part2) != stagingDrawer.IsPartSelected(part))
				{
					stagingDrawer.TogglePartSelected(part2, playSound: false, createNewStep: false);
				}
			}
		}
		else
		{
			selector.ToggleSelected(hit.part);
			SoundPlayer.main.pickupSound.Play();
			TryDoubleClick(hit.part);
		}
	}

	public void OnAreaSelect(Part[] parts)
	{
		if (IsEditingStage)
		{
			Undo.main.CreateNewStep("Area select");
			foreach (Part part in parts)
			{
				stagingDrawer.AddPartSelected(part, playDenySound: false, createNewStep: false);
			}
		}
		else
		{
			BuildManager.main.selector.Select(parts.Select((Part x) => x).Distinct().ToArray());
		}
	}

	private void TryDoubleClick(Part hit)
	{
		if (lastClickTime + 0.3f > Time.unscaledTime && lastClickedPart == hit)
		{
			new JointGroup(RocketManager.GenerateJoints(BuildManager.main.buildGrid.activeGrid.partsHolder.GetArray()), BuildManager.main.buildGrid.activeGrid.partsHolder.parts).RecreateGroups(out var newGroups);
			foreach (JointGroup item in newGroups)
			{
				if (item.parts.Contains(hit))
				{
					Part[] array = item.parts.ToArray();
					if (array.All((Part p) => selector.selected.Contains(p)))
					{
						selector.Deselect(array);
					}
					else
					{
						selector.Select(array);
					}
					PopupManager.MarkUsed(PopupManager.Feature.DoubleClickSelect);
					break;
				}
			}
		}
		lastClickTime = Time.unscaledTime;
		lastClickedPart = hit;
	}

	private void Copy()
	{
		Part[] selectedParts = BuildManager.main.buildGrid.GetSelectedParts();
		if (selectedParts.Length != 0)
		{
			PartSave[] array = PartSave.CreateSaves(selectedParts);
			PartSave[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].burns = null;
			}
			clipboard = array;
			offset = 0.5f;
		}
	}

	private void Paste()
	{
		if (clipboard != null)
		{
			Part[] array = PartsLoader.CreateParts(clipboard, null, BuildManager.main.holdGrid.holdGrid.selectedLayer, OnPartNotOwned.Delete, out var _).ToArray();
			Part[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].transform.position += new Vector3((BuildOrientation.main.rotation != 0f) ? (0f - offset) : offset, offset);
			}
			BuildManager.main.selector.DeselectAll();
			BuildManager.main.holdGrid.StartImportOrDuplicate(array, forDuplicate: true, applyUndo: true, null);
			offset += 0.5f;
		}
	}
}
