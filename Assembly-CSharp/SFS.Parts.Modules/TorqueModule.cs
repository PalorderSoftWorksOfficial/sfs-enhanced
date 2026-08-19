using SFS.Translations;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class TorqueModule : MonoBehaviour, I_PartMenu
{
	public new Bool_Reference enabled = new Bool_Reference
	{
		Value = true
	};

	public Composed_Float torque;

	public bool showDescription = true;

	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		if (showDescription)
		{
			drawer.DrawStat(80, Loc.main.Torque_Module_Torque.Inject(torque.Value.ToString(), "value"));
		}
	}
}
