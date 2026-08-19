using SFS.Translations;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.Parts.Modules;

public class HeatInfoModule : MonoBehaviour, I_PartMenu
{
	void I_PartMenu.Draw(StatsMenu drawer, PartDrawSettings settings)
	{
		float heatTolerance = GetComponent<HeatModuleBase>().HeatTolerance;
		drawer.DrawStat(90, Loc.main.Max_Heat_Tolerance.Inject(heatTolerance.ToTemperatureString(), "temperature"));
	}
}
