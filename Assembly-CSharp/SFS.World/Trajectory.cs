using System;
using System.Collections.Generic;
using System.Linq;

namespace SFS.World;

[Serializable]
public class Trajectory
{
	public List<I_Path> paths;

	public static Trajectory Empty => new Trajectory(new List<I_Path>());

	private int ConicSectionsCount => VideoSettingsPC.main.settings.orbitLinesCount;

	public Trajectory(List<I_Path> paths)
	{
		this.paths = paths;
	}

	public Trajectory(I_Path path)
	{
		paths = new List<I_Path> { path };
	}

	public static Trajectory CreateTrajectory(Location location)
	{
		Trajectory trajectory = new Trajectory(CreatePath(location));
		trajectory.CalculatePaths();
		return trajectory;
	}

	public static Trajectory CreateStationaryTrajectory(StaticWorldObject staticObject)
	{
		return new Trajectory(Stationary.CreatePath(staticObject.location.Value));
	}

	public static I_Path CreatePath(Location location)
	{
		Orbit result = Orbit.TryCreateOrbit(location, calculateTimeParameters: true, calculateEncounters: true, out var success);
		if (success)
		{
			return result;
		}
		return Stationary.CreatePath(location);
	}

	public Location GetLocation(double time)
	{
		return paths[0].GetLocation(time);
	}

	public double GetPathEndTime()
	{
		return paths[0].PathEndTime;
	}

	public double GetStopTimewarpTime(double timeOld, double timeNew)
	{
		return paths[0].GetStopTimewarpTime(timeOld, timeNew);
	}

	public void CheckPathTransition(double time)
	{
		if (time > GetPathEndTime())
		{
			EnterNextPath();
		}
	}

	public void CheckEncounters()
	{
		if (paths.Last().UpdateEncounters())
		{
			CalculatePaths();
		}
	}

	public void EnterNextPath()
	{
		if (paths.Count != 1)
		{
			paths.RemoveAt(0);
			CalculatePaths();
		}
	}

	private void CalculatePaths()
	{
		I_Path nextPath;
		while (paths.Count < ConicSectionsCount && GetNextPath(out nextPath))
		{
			paths.Add(nextPath);
		}
	}

	private bool GetNextPath(out I_Path nextPath)
	{
		List<I_Path> list = paths;
		I_Path i_Path = list[list.Count - 1];
		if (i_Path is Orbit && i_Path.NextPlanet != null)
		{
			if (i_Path.PathType == PathType.Escape)
			{
				if (paths.Count > 1)
				{
					List<I_Path> list2 = paths;
					if (list2[list2.Count - 2].PathType == PathType.Encounter && ConicSectionsCount == 3)
					{
						nextPath = null;
						return false;
					}
				}
				Location location = i_Path.GetLocation(i_Path.PathEndTime);
				Location location2 = i_Path.Planet.orbit.GetLocation(i_Path.PathEndTime);
				nextPath = CreatePath(location + location2);
				return true;
			}
			if (i_Path.PathType == PathType.Encounter)
			{
				Location location3 = i_Path.GetLocation(i_Path.PathEndTime);
				Location location4 = i_Path.NextPlanet.orbit.GetLocation(i_Path.PathEndTime);
				Double2 position = location3.position - location4.position;
				Double2 velocity = location3.velocity - location4.velocity;
				nextPath = CreatePath(new Location(i_Path.PathEndTime, i_Path.NextPlanet, position, velocity));
				return true;
			}
		}
		nextPath = null;
		return false;
	}
}
