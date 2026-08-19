using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Parts;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class Staging : ObservableMonoBehaviour
{
	public Rocket rocket;

	public Bool_Local editMode;

	public List<Stage> stages = new List<Stage>();

	public Action<Stage, int> onStageAdded;

	public Action<Stage> onStageRemoved;

	private void SetStages(List<Stage> newStages, bool record)
	{
		ClearStages(record);
		foreach (Stage newStage in newStages)
		{
			InsertStage(newStage, record);
		}
	}

	public void ClearStages(bool record)
	{
		while (stages.Count > 0)
		{
			RemoveStage(stages.Last(), record);
		}
	}

	public void InsertStage(Stage a, bool record, int index = -1)
	{
		if (record)
		{
			(Undo.GridType, int)[] forUndo = GetForUndo(a);
			Undo.main.RecordAction(new Undo.AddStage(stages.IsValidInsert(index) ? index : stages.Count, add: true, a.stageId, forUndo));
		}
		if (stages.IsValidInsert(index))
		{
			stages.Insert(index, a);
		}
		else
		{
			stages.Add(a);
		}
		onStageAdded?.Invoke(a, index);
	}

	public void RemoveStage(Stage a, bool record)
	{
		if (!stages.Contains(a))
		{
			Debug.LogError("X");
			return;
		}
		if (record)
		{
			(Undo.GridType, int)[] forUndo = GetForUndo(a);
			Undo.main.RecordAction(new Undo.AddStage(stages.IndexOf(a), add: false, a.stageId, forUndo));
		}
		stages.Remove(a);
		onStageRemoved?.Invoke(a);
	}

	public static void CreateStages(StageSave[] stages, Part[] parts)
	{
		foreach (StageSave obj in stages)
		{
			List<Rocket> list = new List<Rocket>();
			int[] partIndexes = obj.partIndexes;
			foreach (int num in partIndexes)
			{
				if (num == -1)
				{
					continue;
				}
				Part part = parts[num];
				if (!(part == null))
				{
					Rocket rocket = part.Rocket;
					if (!list.Contains(rocket))
					{
						rocket.staging.InsertStage(new Stage(rocket.staging.stages.Count + 1, new List<Part>()), record: false);
						list.Add(rocket);
					}
					rocket.staging.stages.Last().AddPart(part, record: false, createNewStep: false);
				}
			}
		}
	}

	public static void OnSplit(Rocket parentRocket, Rocket childRocket)
	{
		childRocket.staging.editMode.Value = parentRocket.staging.editMode.Value;
		foreach (Stage stage in parentRocket.staging.stages)
		{
			bool usedToHaveParts = (stage.usedToHaveParts = stage.PartCount > 0);
			childRocket.staging.InsertStage(new Stage(stage.stageId, new List<Part>())
			{
				usedToHaveParts = usedToHaveParts,
				useStageIdentifier = stage.useStageIdentifier
			}, record: false);
		}
		for (int i = 0; i < parentRocket.staging.stages.Count; i++)
		{
			Part[] array = parentRocket.staging.stages[i].parts.ToArray();
			foreach (Part part in array)
			{
				if (childRocket.jointsGroup.parts.Contains(part))
				{
					parentRocket.staging.stages[i].RemovePart(part, record: false);
					childRocket.staging.stages[i].AddPart(part, record: false, createNewStep: false);
				}
			}
		}
		OnFrameEnd main = OnFrameEnd.main;
		main.onBeforeFrameEnd_Once = (Action)Delegate.Combine(main.onBeforeFrameEnd_Once, new Action(parentRocket.staging.RemoveEmptyStages));
		OnFrameEnd main2 = OnFrameEnd.main;
		main2.onBeforeFrameEnd_Once = (Action)Delegate.Combine(main2.onBeforeFrameEnd_Once, new Action(childRocket.staging.RemoveEmptyStages));
	}

	public static void OnMerge(Rocket A, Rocket B)
	{
		foreach (Stage stage in B.staging.stages)
		{
			A.staging.InsertStage(stage, record: false);
		}
	}

	private void RemoveEmptyStages()
	{
		for (int num = stages.Count - 1; num >= 0; num--)
		{
			if (stages[num].PartCount == 0 && (!rocket.isPlayer.Value || stages[num].usedToHaveParts))
			{
				RemoveStage(stages[num], record: false);
			}
			else
			{
				stages[num].usedToHaveParts = false;
			}
		}
	}

	public void ApplyReorder(Dictionary<Stage, int> order)
	{
		if (BuildManager.main != null)
		{
			Undo.main.CreateNewStep("Reorder Stages");
			for (int num = stages.Count - 1; num >= 0; num--)
			{
				Stage stage = stages[num];
				(Undo.GridType, int)[] forUndo = GetForUndo(stage);
				Undo.main.RecordAction(new Undo.AddStage(num, add: false, stage.stageId, forUndo));
			}
		}
		stages.Sort((Stage a, Stage b) => order[a].CompareTo(order[b]));
		if (BuildManager.main != null)
		{
			for (int num2 = 0; num2 < stages.Count; num2++)
			{
				Stage stage2 = stages[num2];
				(Undo.GridType, int)[] forUndo2 = GetForUndo(stage2);
				Undo.main.RecordAction(new Undo.AddStage(num2, add: true, stage2.stageId, forUndo2));
			}
		}
	}

	public void Load(StageSave[] stageSaves, Part[] parts, bool record)
	{
		SetStages(stageSaves.Select(delegate(StageSave stageSave)
		{
			Stage stage = new Stage(stageSave.stageId, new List<Part>());
			if (stageSave.partIndexes != null)
			{
				int[] partIndexes = stageSave.partIndexes;
				foreach (int num in partIndexes)
				{
					if (parts.IsValidIndex(num))
					{
						stage.AddPart(parts[num], record: false, createNewStep: false);
					}
				}
			}
			return stage;
		}).ToList(), record);
	}

	private (Undo.GridType gridType, int gridPointerIndex)[] GetForUndo(Stage a)
	{
		(Undo.GridType, int)[] array = new(Undo.GridType, int)[a.parts.Count];
		for (int i = 0; i < a.parts.Count; i++)
		{
			Undo.GridType partGridType = GetPartGridType(a.parts[i], out var index);
			array[i] = (partGridType, index);
		}
		return array;
	}

	public static Undo.GridType GetPartGridType(Part part, out int index)
	{
		PartHolder partsHolder = BuildManager.main.buildGrid.activeGrid.partsHolder;
		if (partsHolder.ContainsPart(part))
		{
			index = partsHolder.parts.IndexOf(part);
			return Undo.GridType.ActiveGrid;
		}
		PartHolder partsHolder2 = BuildManager.main.buildGrid.inactiveGrid.partsHolder;
		if (partsHolder2.ContainsPart(part))
		{
			index = partsHolder2.parts.IndexOf(part);
			return Undo.GridType.InactiveGrid;
		}
		PartHolder partsHolder3 = BuildManager.main.holdGrid.holdGrid.partsHolder;
		if (partsHolder3.ContainsPart(part))
		{
			index = partsHolder3.parts.IndexOf(part);
			return Undo.GridType.HoldGrid;
		}
		throw new Exception();
	}
}
