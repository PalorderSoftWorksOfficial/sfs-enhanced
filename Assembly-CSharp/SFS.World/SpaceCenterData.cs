using System;
using SFS.WorldBase;

namespace SFS.World;

[Serializable]
public class SpaceCenterData
{
	[Serializable]
	public class BuildingPosition
	{
		public double horizontalPosition;

		public double height;

		public BuildingPosition(double horizontalPosition, double height)
		{
			this.horizontalPosition = horizontalPosition;
			this.height = height;
		}

		public Double2 GetPosition(double planetRadius, double originAngle)
		{
			return Double2.CosSin(0.01745329238474369 * originAngle - horizontalPosition / planetRadius, planetRadius + height);
		}
	}

	public string address = "Earth";

	public double angle = 90.0;

	public BuildingPosition position_LaunchPad = new BuildingPosition(365.0, 26.2);

	public Planet Planet => address.GetPlanet();

	public Location LaunchPadLocation => new Location(address.GetPlanet(), position_LaunchPad.GetPosition(Planet.Radius, angle));

	public static SpaceCenterData FromLegacyLaunchLocation(LegacyLaunchPad launchPad)
	{
		SpaceCenterData obj = new SpaceCenterData
		{
			address = launchPad.address,
			angle = 90.0
		};
		double radius = launchPad.address.GetPlanet().Radius;
		obj.position_LaunchPad.horizontalPosition = (1.5707963705062866 - launchPad.position.AngleRadians) * radius;
		obj.position_LaunchPad.height = launchPad.position.magnitude - radius;
		return obj;
	}
}
