using System;
using System.Linq;
using SFS.Parts.Modules;
using SFS.UI;
using UnityEngine;

namespace SFS.World;

public class FlightInfoDrawer : MonoBehaviour
{
	public TextAdapter timewarpText;

	public TextAdapter massText;

	public TextAdapter thrustText;

	public TextAdapter thrustToWeightText;

	public TextAdapter partCountText;

	public GameObject menuHolder;

	private void Update()
	{
		if (PlayerController.main.player.Value is Rocket rocket)
		{
			menuHolder.SetActive(value: true);
			float mass = rocket.rb2d.mass;
			float num = rocket.partHolder.GetModules<EngineModule>().Sum((EngineModule a) => a.thrust.Value * a.throttle_Out.Value) + rocket.partHolder.GetModules<BoosterModule>().Sum((BoosterModule b) => b.thrustVector.Value.magnitude * b.throttle_Out.Value);
			massText.Text = mass.ToMassString(forceDecimal: true).Split(':', StringSplitOptions.None)[1];
			thrustText.Text = num.ToThrustString().Split(':', StringSplitOptions.None)[1];
			thrustToWeightText.Text = (num / mass).ToTwrString().Split(':', StringSplitOptions.None)[1];
			partCountText.Text = rocket.partHolder.parts.Count.ToString();
		}
		else
		{
			massText.Text = 0f.ToMassString(forceDecimal: true).Split(':', StringSplitOptions.None)[1];
			thrustText.Text = 0f.ToThrustString().Split(':', StringSplitOptions.None)[1];
			thrustToWeightText.Text = 0f.ToTwrString().Split(':', StringSplitOptions.None)[1];
			partCountText.Text = 0.ToString();
		}
		timewarpText.Text = WorldTime.main.timewarpSpeed + "x";
	}
}
