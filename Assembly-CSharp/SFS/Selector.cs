using System;
using System.Collections.Generic;
using SFS.Parts;
using UnityEngine;

namespace SFS;

public class Selector : MonoBehaviour
{
	public Action onSelectedChange;

	public HashSet<Part> selected = new HashSet<Part>();

	public void ToggleSelected(Part A)
	{
		if (selected.Contains(A))
		{
			Deselect(A);
		}
		else
		{
			Select(A);
		}
	}

	public void Select(params Part[] parts)
	{
		foreach (Part item in parts)
		{
			if (!selected.Contains(item))
			{
				selected.Add(item);
			}
		}
		onSelectedChange?.Invoke();
	}

	public void Deselect(params Part[] parts)
	{
		foreach (Part item in parts)
		{
			if (selected.Contains(item))
			{
				selected.Remove(item);
			}
		}
		onSelectedChange?.Invoke();
	}

	public void DeselectAll()
	{
		selected.Clear();
		onSelectedChange?.Invoke();
	}
}
