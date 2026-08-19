using System;
using System.Collections.Generic;
using SFS.Translations;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

[RequireComponent(typeof(Button))]
public class CycleSelector : MonoBehaviour
{
	public IntEvent cycleEvent;

	public Text text;

	public List<TranslationVariable> options;

	private int selectedIndex;

	public int SelectedIndex
	{
		get
		{
			return selectedIndex;
		}
		set
		{
			if (options.Count != 0)
			{
				selectedIndex = value % options.Count;
				text.text = options[selectedIndex].Field;
				cycleEvent?.Invoke(selectedIndex);
			}
		}
	}

	private void Start()
	{
		GetComponent<Button>().onClick += new Action(OnClick);
		SelectedIndex = 0;
	}

	private void OnDestroy()
	{
		GetComponent<Button>().onClick -= new Action(OnClick);
	}

	private void OnClick()
	{
		SelectedIndex++;
	}
}
