using System;
using UnityEngine;

namespace SFS.UI;

public class RadioGroup : MonoBehaviour
{
	private RadioButton selected;

	public Action<RadioButton> onSelectionChanged;

	public RadioButton Selected
	{
		get
		{
			return selected;
		}
		set
		{
			selected = value;
			onSelectionChanged?.Invoke(selected);
		}
	}

	public Button SelectedButton
	{
		get
		{
			if (!(selected != null))
			{
				return null;
			}
			return selected.GetComponent<Button>();
		}
	}
}
