using System;
using SFS.WorldBase;

namespace SFS.World;

[Serializable]
public class Stationary : I_Path
{
	private Location location;

	Planet I_Path.Planet => location.planet;

	Planet I_Path.NextPlanet => null;

	double I_Path.PathEndTime => double.PositiveInfinity;

	PathType I_Path.PathType => PathType.Eternal;

	public static Stationary CreatePath(Location location)
	{
		return new Stationary
		{
			location = location
		};
	}

	Location I_Path.GetLocation(double time)
	{
		return new Location(time, location.planet, location.position, location.velocity);
	}

	double I_Path.GetStopTimewarpTime(double timeOld, double timeNew)
	{
		return double.PositiveInfinity;
	}

	bool I_Path.UpdateEncounters()
	{
		return false;
	}
}
