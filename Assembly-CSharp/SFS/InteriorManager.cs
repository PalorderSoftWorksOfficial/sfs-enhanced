using System;
using SFS.Builds;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using UnityEngine;

namespace SFS;

public class InteriorManager : MonoBehaviour
{
	public static InteriorManager main;

	public Bool_Local interiorView;

	public ButtonPC toggleButton;

	public RectTransform interiorViewDisabled;

	public RectTransform interiorViewEnabled;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		interiorView.OnChange += new Action(UpdateUI);
	}

	public void ToggleInteriorView()
	{
		if (BuildManager.main != null)
		{
			Undo.main.RecordOtherStep(interiorView.Value ? Undo.OtherAction.Type.DisableInteriorView : Undo.OtherAction.Type.EnableInteriorView);
		}
		interiorView.Value = !interiorView.Value;
		MsgDrawer.main.Log(interiorView.Value ? Loc.main.Interior_View_On : Loc.main.Interior_View_Off);
	}

	private void UpdateUI()
	{
		if (!(BuildManager.main == null))
		{
			toggleButton.SetSelected(!interiorView.Value);
		}
	}
}
