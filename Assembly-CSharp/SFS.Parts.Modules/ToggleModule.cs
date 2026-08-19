using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Translations;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ToggleModule : MonoBehaviour
{
	public TranslationVariable label;

	public MoveModule state;

	private void Start()
	{
		if (BuildManager.main != null)
		{
			state.animationTime = 0.5f;
		}
	}

	public void Draw(List<ToggleModule> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.build || settings.game)
		{
			drawer.DrawToggle(-1, () => label.Field, OnToggle, () => state.targetTime.Value > 0f, delegate(Action update)
			{
				state.targetTime.OnChange += update;
			}, delegate(Action update)
			{
				state.targetTime.OnChange -= update;
			});
		}
		void OnToggle()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				state.Toggle();
				float value = state.targetTime.Value;
				foreach (ToggleModule module in modules)
				{
					module.state.targetTime.Value = value;
				}
			});
		}
	}
}
