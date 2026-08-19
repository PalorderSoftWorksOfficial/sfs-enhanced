using System;
using SFS.Input;
using UnityEngine;

namespace SFS.UI;

[RequireComponent(typeof(Button))]
public class RadioButton : MonoBehaviour
{
	public RadioGroup group;

	public GameObject selectedIndicator;

	public bool loseSelectOnDisable = true;

	private bool IsSelected => this == group.Selected;

	public void Select()
	{
		if (group.Selected != null)
		{
			group.Selected.selectedIndicator.SetActive(value: false);
		}
		group.Selected = (IsSelected ? null : this);
		selectedIndicator.SetActive(IsSelected);
	}

	private void Awake()
	{
		selectedIndicator.SetActive(value: false);
		GetComponent<Button>().onClick += new Action<OnInputEndData>(OnClick);
	}

	private void OnDisable()
	{
		if (loseSelectOnDisable && IsSelected)
		{
			selectedIndicator.SetActive(value: false);
			group.Selected = null;
		}
	}

	private void OnClick(OnInputEndData data)
	{
		Select();
	}
}
