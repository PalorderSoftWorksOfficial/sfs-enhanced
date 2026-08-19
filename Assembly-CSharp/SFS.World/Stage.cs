using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Parts;
using UnityEngine;

namespace SFS.World;

[Serializable]
public class Stage
{
	public int stageId;

	public List<Part> parts;

	public bool usedToHaveParts;

	public Action<Part, int> onPartInserted;

	public Action<int> onPartRemoved;

	public int useStageIdentifier;

	public int PartCount => parts.Count;

	public bool Contains(Part part)
	{
		return parts.Contains(part);
	}

	public Stage(int stageId, List<Part> parts)
	{
		this.stageId = stageId;
		this.parts = parts;
		foreach (Part part in parts)
		{
			if (BuildManager.main != null)
			{
				part.aboutToDestroy = (Action<Part>)Delegate.Combine(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
			}
			else
			{
				part.onPartDestroyed = (Action<Part>)Delegate.Combine(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
			}
		}
	}

	public void ToggleSelected(Part part, bool crateNewStep)
	{
		if (crateNewStep)
		{
			Undo.main.CreateNewStep("Toggle Selected");
		}
		if (!parts.Contains(part))
		{
			AddPart(part, record: true, createNewStep: false);
		}
		else
		{
			RemovePart(part, record: true);
		}
	}

	public void AddPart(Part part, bool record, bool createNewStep)
	{
		if (createNewStep)
		{
			Undo.main.CreateNewStep("Add Part");
		}
		InsertPart(part, parts.Count, record);
	}

	public void InsertPart(Part part, int index, bool record)
	{
		if (parts.Contains(part))
		{
			return;
		}
		if ((BuildManager.main != null) & record)
		{
			int num = BuildManager.main.buildMenus.staging.stages.IndexOf(this);
			if (num == -1)
			{
				Debug.LogError("X");
			}
			Undo.main.RecordAction(new Undo.AddPartToStage(num, index, Staging.GetPartGridType(part, out var index2), add: true, index2));
		}
		parts.Insert(index, part);
		if (BuildManager.main != null)
		{
			part.aboutToDestroy = (Action<Part>)Delegate.Combine(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
		}
		else
		{
			part.onPartDestroyed = (Action<Part>)Delegate.Combine(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
		}
		onPartInserted?.Invoke(part, index);
	}

	public void RemovePart(Part part, bool record)
	{
		if (!parts.Contains(part))
		{
			return;
		}
		if ((BuildManager.main != null) & record)
		{
			int num = BuildManager.main.buildMenus.staging.stages.IndexOf(this);
			if (num == -1)
			{
				Debug.LogError("X");
			}
			else
			{
				Undo.main.RecordAction(new Undo.AddPartToStage(num, parts.IndexOf(part), Staging.GetPartGridType(part, out var index), add: false, index));
			}
		}
		int obj = parts.IndexOf(part);
		parts.Remove(part);
		if (BuildManager.main != null)
		{
			part.aboutToDestroy = (Action<Part>)Delegate.Remove(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
		}
		else
		{
			part.onPartDestroyed = (Action<Part>)Delegate.Remove(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
		}
		onPartRemoved?.Invoke(obj);
	}

	private void OnPartDestroyed(Part part)
	{
		RemovePart(part, record: true);
		usedToHaveParts = false;
	}

	public void SetPartAtIndex(int index, Part part)
	{
		parts[index] = part;
		if (BuildManager.main != null)
		{
			part.aboutToDestroy = (Action<Part>)Delegate.Combine(part.aboutToDestroy, new Action<Part>(OnPartDestroyed));
		}
		else
		{
			part.onPartDestroyed = (Action<Part>)Delegate.Combine(part.onPartDestroyed, new Action<Part>(OnPartDestroyed));
		}
	}
}
