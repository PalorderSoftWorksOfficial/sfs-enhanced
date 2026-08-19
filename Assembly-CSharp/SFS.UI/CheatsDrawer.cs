using System;
using SFS.Translations;
using SFS.World;
using UnityEngine;

namespace SFS.UI;

public class CheatsDrawer : MonoBehaviour
{
	public TextAdapter cheatsText;

	private void Start()
	{
		Loc.OnChange += new Action(UpdateText);
		SandboxSettings.main.onToggleCheat += new Action(UpdateText);
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(UpdateText);
		SandboxSettings.main.onToggleCheat -= new Action(UpdateText);
	}

	private void UpdateText()
	{
		string text = string.Empty;
		SandboxSettings.Data settings = SandboxSettings.main.settings;
		if (settings.infiniteFuel)
		{
			Add(Loc.main.Infinite_Fuel_Name);
		}
		if (settings.noGravity)
		{
			Add(Loc.main.No_Gravity_Name);
		}
		if (settings.noAtmosphericDrag)
		{
			Add(Loc.main.No_Atmospheric_Drag_Name);
		}
		if (settings.unbreakableParts)
		{
			Add(Loc.main.No_Collision_Damage_Name);
		}
		if (settings.noHeatDamage)
		{
			Add(Loc.main.No_Heat_Damage_Name);
		}
		if (settings.noBurnMarks)
		{
			Add(Loc.main.No_Burn_Marks_Name);
		}
		cheatsText.Text = text;
		base.gameObject.SetActive(!string.IsNullOrEmpty(cheatsText.Text));
		void Add(Field t)
		{
			text = string.Concat(text, t, "\n");
		}
	}
}
