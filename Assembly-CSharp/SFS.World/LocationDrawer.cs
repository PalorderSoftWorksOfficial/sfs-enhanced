using System;
using System.Collections.Generic;
using SFS.Translations;
using SFS.UI;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class LocationDrawer : MonoBehaviour
{
	public static LocationDrawer main;

	public TextAdapter heightText;

	public TextAdapter velocityText;

	public TextAdapter heightTextSide;

	public TextAdapter velocityTextSide;

	public TextAdapter angleText;

	public TextAdapter heightTitle;

	public TextAdapter velocityTitle;

	public AngleInfo currentAngleInfo;

	private void Awake()
	{
		main = this;
	}

	private void Update()
	{
		bool flag = false;
		Location location = ((PlayerController.main.player.Value != null) ? PlayerController.main.player.Value.location.Value : WorldView.main.ViewLocation);
		SelectableObject target = Map.navigation.target;
		string text;
		string text2;
		if (target is MapRocket && (location.position - target.Location.position).Mag_LessThan(10000.0))
		{
			text = (flag ? Loc.main.Velocity_Relative_Vertical : Loc.main.Velocity_Relative_Horizontal).Inject((location.velocity - target.Location.velocity).magnitude.ToVelocityString(), "speed");
			text2 = (flag ? Loc.main.Distance_Relative_Vertical : Loc.main.Distance_Relative_Horizontal).Inject((location.position - target.Location.position).magnitude.ToDistanceString(), "distance");
		}
		else
		{
			text = (flag ? Loc.main.Velocity_Vertical : Loc.main.Velocity_Horizontal).Inject(location.velocity.magnitude.ToVelocityString(), "speed");
			text2 = ((!(location.TerrainHeight < 2000.0) && !(location.Height < 500.0)) ? ((string)(flag ? Loc.main.Height_Vertical : Loc.main.Height_Horizontal).Inject(location.Height.ToDistanceString(), "height")) : ((string)(flag ? Loc.main.Height_Terrain_Vertical : Loc.main.Height_Terrain_Horizontal).Inject(location.TerrainHeight.ToDistanceString(), "height")));
		}
		GetIdealAngle(out var showAngle, out var angle, out var targetAngle);
		currentAngleInfo = new AngleInfo(showAngle, angle, targetAngle);
		int num = text2.IndexOf(":", 0, StringComparison.Ordinal);
		heightTitle.Text = text2.Substring(0, num);
		heightText.Text = text2.Substring(num, text2.Length - num).Replace(": ", "");
		int num2 = text.IndexOf(":", 0, StringComparison.Ordinal);
		velocityTitle.Text = text.Substring(0, num2);
		velocityText.Text = text.Substring(num2, text.Length - num2).Replace(": ", "");
	}

	private static void GetIdealAngle(out bool showAngle, out double angle, out double targetAngle)
	{
		showAngle = false;
		angle = 0.0;
		targetAngle = 0.0;
		if (PlayerController.main.player.Value == null || !(PlayerController.main.player.Value is Rocket rocket))
		{
			return;
		}
		List<I_Path> paths = rocket.physics.GetTrajectory().paths;
		if (paths.Count == 0 || paths[0].Planet.codeName != "Earth" || !(paths[0] is Orbit orbit))
		{
			return;
		}
		double num = orbit.apoapsis - orbit.Planet.Radius;
		if (!(num > orbit.Planet.data.atmospherePhysics.height) && !(rocket.location.velocity.Value.Rotate(0.0 - rocket.location.position.Value.AngleRadians + Math.PI / 2.0).y < 5.0))
		{
			showAngle = true;
			angle = 90f - rocket.GetRotation();
			while (angle < -180.0)
			{
				angle += 360.0;
			}
			while (angle > 180.0)
			{
				angle -= 360.0;
			}
			num /= 1.1;
			double num2 = Base.worldBase.settings.difficulty.AtmosphereScale(paths[0].Planet.data);
			targetAngle = ((!(num < num2 * 300.0)) ? ((num < num2 * 1000.0) ? 5 : ((num < num2 * 2500.0) ? 10 : ((num < num2 * 4500.0) ? 20 : ((num < num2 * 7500.0) ? 30 : ((num < num2 * 12000.0) ? 45 : ((num < num2 * 20000.0) ? 60 : 75)))))) : 0);
			if (angle < 0.0)
			{
				targetAngle = 0.0 - targetAngle;
			}
		}
	}
}
