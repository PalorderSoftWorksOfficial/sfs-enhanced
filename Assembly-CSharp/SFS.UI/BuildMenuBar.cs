using System;
using SFS.Builds;
using SFS.Input;
using SFS.Translations;
using UnityEngine;

namespace SFS.UI;

public class BuildMenuBar : MonoBehaviour
{
	public ButtonPC saveButton;

	public ButtonPC loadButton;

	public ButtonPC newButton;

	private void Start()
	{
		newButton.onClick += (Action)delegate
		{
			MenuGenerator.OpenConfirmation(CloseMode.Stack, () => Loc.main.Clear_Warning, () => Loc.main.Clear_Confirm, delegate
			{
				Undo.main.CreateNewStep("clear");
				BuildState.main.Clear(applyUndo: true);
			});
		};
		saveButton.onClick += (Action)delegate
		{
			Menu.load.OpenSaveMenu();
		};
		loadButton.onClick += (Action)delegate
		{
			Menu.load.Open();
		};
	}
}
