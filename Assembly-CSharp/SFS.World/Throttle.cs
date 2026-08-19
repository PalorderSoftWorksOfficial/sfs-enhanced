using System;
using SFS.Variables;

namespace SFS.World;

public class Throttle : ObservableMonoBehaviour
{
	public Bool_Local throttleOn;

	public Float_Local throttlePercent;

	public Float_Local output_Throttle;

	private void Start()
	{
		throttleOn.OnChange += new Action(UpdateThrottle);
		throttlePercent.OnChange += new Action(UpdateThrottle);
	}

	private void UpdateThrottle()
	{
		output_Throttle.Value = (throttleOn.Value ? throttlePercent.Value : 0f);
	}
}
