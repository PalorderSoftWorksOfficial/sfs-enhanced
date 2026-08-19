using System;
using System.Collections.Generic;
using SFS.UI;
using UnityEngine;
using UnityEngine.Events;

public class ButtonGroup : MonoBehaviour
{
	public List<Button> buttons;

	public UnityEvent onSelectedChange;

	public int SelectedIndex { get; private set; }

	private void Start()
	{
		SetSelectedIndex(SelectedIndex);
		buttons.ForEach(delegate(Button b)
		{
			b.onClick += (Action)delegate
			{
				ButtonClicked(b);
			};
		});
	}

	private void ButtonClicked(Button selectedButton)
	{
		buttons.ForEach(delegate(Button b)
		{
			if (b is ButtonPC buttonPC)
			{
				buttonPC.SetSelected(b == selectedButton);
			}
			else
			{
				b.transform.Find("Outline").gameObject.SetActive(b == selectedButton);
			}
		});
		SelectedIndex = buttons.IndexOf(selectedButton);
		onSelectedChange.Invoke();
	}

	public void SetSelectedIndex(int index)
	{
		ButtonClicked(buttons.IsValidIndex(index) ? buttons[index] : null);
	}
}
