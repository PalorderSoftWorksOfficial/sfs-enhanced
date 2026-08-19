using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World.Drag;

namespace SFS.World;

public class EVA_Resources : HeatModuleBase
{
	public Astronaut_EVA astronaut;

	public const double Fuel_DeltaV = 200.0;

	public Double_Reference fuelPercent;

	public Float_Reference temperature;

	public override string Name => "";

	public override bool IsHeatShield => false;

	public override float Temperature
	{
		get
		{
			return temperature.Value;
		}
		set
		{
			temperature.Value = value;
		}
	}

	public override int LastAppliedIndex { get; set; } = -1;

	public override float ExposedSurface { get; set; }

	public override float HeatTolerance => AeroModule.GetHeatTolerance(SFS.World.Drag.HeatTolerance.Low);

	public void ConsumeFuel(double amount)
	{
		if (SandboxSettings.main.settings.infiniteFuel)
		{
			return;
		}
		double value = fuelPercent.Value;
		fuelPercent.Value -= amount;
		double[] array = new double[4] { 0.5, 0.25, 0.1, 0.05 };
		foreach (double num in array)
		{
			if (value > num && fuelPercent.Value <= num)
			{
				MsgDrawer.main.Log(Loc.main.Fuel_Running_Out.Inject(num.ToPercentString(forceDecimals: false), "percent"));
				WorldTime.main.StopTimewarp(showMsg: false);
			}
		}
		if (fuelPercent.Value < 0.0)
		{
			fuelPercent.Value = 0.0;
			MsgDrawer.main.Log(Loc.main.Out_Of_Fuel);
		}
	}

	public override void OnOverheat(bool _)
	{
		if (astronaut != null)
		{
			AstronautManager.DestroyEVA(astronaut, death: true);
		}
	}
}
