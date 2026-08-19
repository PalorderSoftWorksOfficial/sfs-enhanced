using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Parts;
using SFS.UI;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class Undo : MonoBehaviour
{
	[Serializable]
	public class Step
	{
		public string name;

		public List<ActionBase> actions = new List<ActionBase>();

		public Step(string name)
		{
			this.name = name;
		}
	}

	[Serializable]
	public class AddPart : ActionBase
	{
		public PartSave[] parts;

		public List<int> partIndexes;

		public bool add;

		public GridType gridType;

		public List<(int stageIndex, int stagePartIndex, int gridIndex)> stageFixes;

		public AddPart(Part[] parts, List<int> partIndexes, GridType gridType, bool add)
		{
			this.parts = PartSave.CreateSaves(parts);
			this.partIndexes = partIndexes;
			this.gridType = gridType;
			this.add = add;
			stageFixes = new List<(int, int, int)>();
			List<Stage> stages = BuildManager.main.buildMenus.staging.stages;
			for (int i = 0; i < parts.Length; i++)
			{
				Part part = parts[i];
				for (int j = 0; j < stages.Count; j++)
				{
					Stage stage = stages[j];
					for (int k = 0; k < stage.parts.Count; k++)
					{
						if (stage.parts[k] == part)
						{
							stageFixes.Add((j, k, partIndexes[i]));
						}
					}
				}
			}
		}
	}

	[Serializable]
	public class AddPartToStage : ActionBase
	{
		public int stageIndex;

		public int partIndex;

		public bool add;

		public int gridPointerIndex;

		public GridType gridType;

		public AddPartToStage(int stageIndex, int partIndex, GridType gridType, bool add, int gridPointerIndex)
		{
			this.stageIndex = stageIndex;
			this.partIndex = partIndex;
			this.gridType = gridType;
			this.add = add;
			this.gridPointerIndex = gridPointerIndex;
		}
	}

	[Serializable]
	public class AddStage : ActionBase
	{
		public int stageIndex;

		public bool add;

		public int stageId;

		public (GridType gridType, int gridPointerIndex)[] parts;

		public AddStage(int stageIndex, bool add, int stageId, (GridType gridType, int gridPointerIndex)[] parts)
		{
			this.stageIndex = stageIndex;
			this.add = add;
			this.stageId = stageId;
			this.parts = parts;
		}
	}

	[Serializable]
	public class OtherAction : ActionBase
	{
		public enum Type
		{
			OpenStaging,
			CloseStaging,
			EnableInteriorView,
			DisableInteriorView
		}

		public Type type;

		public OtherAction(Type type)
		{
			this.type = type;
		}
	}

	[Serializable]
	public abstract class ActionBase
	{
	}

	public enum GridType
	{
		ActiveGrid,
		InactiveGrid,
		HoldGrid
	}

	public static Undo main;

	public Button undoButton;

	public Button redoButton;

	private List<Step> steps = new List<Step>();

	private int undoPointer = -1;

	private Part[] lastParts;

	private bool midUndo;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		KeysNode keysNode = BuildManager.main.build_Input.keysNode;
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Undo, UndoStep);
		keysNode.AddOnKeyDown(KeybindingsPC.keys.Redo, RedoStep);
	}

	public void CreateNewStep(string step)
	{
		if (undoPointer < steps.Count - 1)
		{
			steps.RemoveRange(undoPointer + 1, steps.Count - undoPointer - 1);
		}
		if (steps.Count > 0 && steps.Last().actions.Count == 0)
		{
			steps.RemoveAt(steps.Count - 1);
			undoPointer--;
		}
		CloseOngoingStep();
		steps.Add(new Step(step));
		if (steps.Count > 30)
		{
			steps.RemoveAt(0);
		}
		else
		{
			undoPointer++;
		}
	}

	public void RecordStatChangeStep<T>(List<T> modules, Action action, bool createStep = true) where T : MonoBehaviour
	{
		List<Part> list = new List<Part>();
		foreach (T module in modules)
		{
			Part componentInParent = module.GetComponentInParent<Part>();
			if (!list.Contains(componentInParent))
			{
				list.Add(componentInParent);
			}
		}
		RecordStatChangeStep(list.ToArray(), action, createStep);
	}

	public void RecordOtherStep(OtherAction.Type type)
	{
		CreateNewStep(type.ToString());
		RecordAction(new OtherAction(type));
	}

	public void RecordStatChangeStep(Part[] parts, Action action, bool createStep = true)
	{
		if (createStep)
		{
			CreateNewStep("Stat change");
		}
		if (lastParts != null && !lastParts.SequenceEqual(parts))
		{
			CloseOngoingStep();
		}
		if (lastParts == null)
		{
			RecordRemoveParts(parts, add: false, alwaysSave: true);
			lastParts = parts;
		}
		action();
	}

	private void CloseOngoingStep()
	{
		if (lastParts != null)
		{
			Part[] parts = lastParts;
			lastParts = null;
			RecordRemoveParts(parts, add: true, alwaysSave: true);
		}
	}

	public void RecordRemoveParts(Part[] parts, bool add, bool alwaysSave)
	{
		CreateUndoAction(BuildManager.main.buildGrid.activeGrid.partsHolder.parts, active: true);
		CreateUndoAction(BuildManager.main.buildGrid.inactiveGrid.partsHolder.parts, active: false);
		void CreateUndoAction(List<Part> list, bool active)
		{
			Dictionary<Part, int> dictionary = new Dictionary<Part, int>();
			for (int i = 0; i < list.Count; i++)
			{
				dictionary.Add(list[i], i);
			}
			List<Part> list2 = new List<Part>();
			List<int> list3 = new List<int>();
			Part[] array = parts;
			foreach (Part part in array)
			{
				if (dictionary.TryGetValue(part, out var value))
				{
					list3.Add(value);
					list2.Add(part);
				}
			}
			if (list2.Count > 0)
			{
				RecordPartAction(new AddPart(list2.ToArray(), list3, (!active) ? GridType.InactiveGrid : GridType.ActiveGrid, add), alwaysSave);
			}
		}
	}

	public void RecordPartAction(AddPart action, bool saveEvenIfZeroParts = false)
	{
		if ((action.parts.Length != 0) | saveEvenIfZeroParts)
		{
			RecordAction(action);
		}
	}

	public void RecordAction(ActionBase action)
	{
		if (midUndo)
		{
			Debug.LogError("Mid undo");
			return;
		}
		CloseOngoingStep();
		List<Step> list = steps;
		list[list.Count - 1].actions.Add(action);
	}

	public void UndoStep()
	{
		if (undoPointer >= 0)
		{
			BuildManager.main.selector.DeselectAll();
			CloseOngoingStep();
			midUndo = true;
			Step step = steps[undoPointer];
			undoPointer--;
			for (int num = step.actions.Count - 1; num >= 0; num--)
			{
				ApplyAction(step.actions[num], undo: true);
			}
			midUndo = false;
		}
	}

	public void RedoStep()
	{
		if (undoPointer > steps.Count - 2)
		{
			return;
		}
		BuildManager.main.selector.DeselectAll();
		CloseOngoingStep();
		midUndo = true;
		undoPointer++;
		foreach (ActionBase action in steps[undoPointer].actions)
		{
			ApplyAction(action, undo: false);
		}
		midUndo = false;
	}

	private void ApplyAction(ActionBase action, bool undo)
	{
		if (!(action is AddPart action2))
		{
			if (!(action is AddPartToStage action3))
			{
				if (!(action is AddStage action4))
				{
					if (action is OtherAction action5)
					{
						ApplyOtherAction(action5, undo);
					}
				}
				else
				{
					ApplyStageAction(action4, undo);
				}
			}
			else
			{
				ApplyStagePartAction(action3, undo);
			}
		}
		else
		{
			ApplyPartAction(action2, undo);
		}
	}

	private void ApplyPartAction(AddPart action, bool undo)
	{
		PartGrid grid = GetGrid(action.gridType);
		if (undo != action.add)
		{
			Part[] array = PartsLoader.CreateParts(action.parts, null, grid.selectedLayer, OnPartNotOwned.Allow, out var _);
			(Part, int)[] array2 = new(Part, int)[action.parts.Length];
			for (int i = 0; i < action.parts.Length; i++)
			{
				array2[i] = (array[i], action.partIndexes[i]);
			}
			array2 = array2.OrderBy(((Part, int index) x) => x.index).ToArray();
			(Part, int)[] array3 = array2;
			for (int num = 0; num < array3.Length; num++)
			{
				var (part, index) = array3[num];
				grid.AddPartAtIndex(index, part);
			}
			{
				foreach (var (index2, index3, index4) in action.stageFixes)
				{
					BuildManager.main.buildMenus.staging.stages[index2].SetPartAtIndex(index3, grid.partsHolder.parts[index4]);
				}
				return;
			}
		}
		int[] array4 = action.partIndexes.ToArray();
		Array.Sort(array4);
		for (int num2 = array4.Length - 1; num2 >= 0; num2--)
		{
			int index5 = array4[num2];
			Part part2 = grid.partsHolder.parts[index5];
			grid.RemovePartAtIndex(index5);
			part2.DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
		}
	}

	private void ApplyStagePartAction(AddPartToStage action, bool undo)
	{
		PartGrid grid = GetGrid(action.gridType);
		Stage stage = BuildManager.main.buildMenus.staging.stages[action.stageIndex];
		if (undo != action.add)
		{
			stage.InsertPart(grid.partsHolder.parts[action.gridPointerIndex], action.partIndex, record: false);
		}
		else
		{
			stage.RemovePart(stage.parts[action.partIndex], record: false);
		}
	}

	private void ApplyStageAction(AddStage action, bool undo)
	{
		if (undo != action.add)
		{
			List<Part> list = new List<Part>();
			(GridType, int)[] parts = action.parts;
			for (int i = 0; i < parts.Length; i++)
			{
				var (type, index) = parts[i];
				list.Add(GetGrid(type).partsHolder.parts[index]);
			}
			BuildManager.main.buildMenus.staging.InsertStage(new Stage(action.stageId, list), record: false, action.stageIndex);
		}
		else
		{
			Stage a = BuildManager.main.buildMenus.staging.stages[action.stageIndex];
			BuildManager.main.buildMenus.staging.RemoveStage(a, record: false);
		}
	}

	private void ApplyOtherAction(OtherAction action, bool undo)
	{
		switch (action.type)
		{
		case OtherAction.Type.OpenStaging:
			BuildManager.main.buildMenus.stagingMode.Value = !undo;
			break;
		case OtherAction.Type.CloseStaging:
			BuildManager.main.buildMenus.stagingMode.Value = undo;
			break;
		case OtherAction.Type.EnableInteriorView:
			InteriorManager.main.interiorView.Value = !undo;
			break;
		case OtherAction.Type.DisableInteriorView:
			InteriorManager.main.interiorView.Value = undo;
			break;
		}
	}

	private PartGrid GetGrid(GridType type)
	{
		return type switch
		{
			GridType.ActiveGrid => BuildManager.main.buildGrid.activeGrid, 
			GridType.InactiveGrid => BuildManager.main.buildGrid.inactiveGrid, 
			GridType.HoldGrid => BuildManager.main.holdGrid.holdGrid, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public void Clear()
	{
		undoPointer = -1;
		CloseOngoingStep();
		steps.Clear();
		midUndo = false;
	}
}
