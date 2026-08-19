using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.Navigation;

public class Iterator_Velocity : Iterator
{
	public delegate Location GetSample(double t);

	public Iterator_Velocity()
		: base(30)
	{
	}

	public Orbit Calculate(GetSample sample, Feedback<Orbit> feedback, Valid<Orbit> valid, bool debug = false)
	{
		Orbit orbit;
		while (true)
		{
			if (base.OutOfIterations)
			{
				return null;
			}
			iterationCount++;
			orbit = new Orbit(sample(base.Current), calculateTimeParameters: true, calculateEncounters: false);
			if (debug)
			{
				orbit.DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 1f, 0.05f));
			}
			if (!(orbit.apoapsis >= orbit.Planet.SOI))
			{
				if (feedback(orbit))
				{
					ApplyIteration();
				}
				if (base.OutOfIterations)
				{
					break;
				}
			}
		}
		if (!valid(orbit))
		{
			return null;
		}
		return orbit;
	}

	public Orbit Calculate_Flyby(GetSample sample, Feedback<Orbit> feedback, Valid<Orbit> valid, bool debug = false)
	{
		Orbit orbit;
		do
		{
			if (base.OutOfIterations)
			{
				return null;
			}
			iterationCount++;
			orbit = new Orbit(sample(base.Current), calculateTimeParameters: true, calculateEncounters: false);
			if (debug)
			{
				orbit.DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 1f, 0.05f));
			}
			if (feedback(orbit))
			{
				ApplyIteration();
			}
		}
		while (!base.OutOfIterations);
		if (!valid(orbit))
		{
			return null;
		}
		return orbit;
	}

	public Orbit[] Calculate_Escape(GetSample sample, Feedback<Orbit[]> feedback, Valid<Orbit[]> valid, bool debug = false)
	{
		iterationMax = 35;
		Orbit[] array;
		while (true)
		{
			if (base.OutOfIterations)
			{
				return null;
			}
			iterationCount++;
			Orbit orbit = new Orbit(sample(base.Current), calculateTimeParameters: true, calculateEncounters: false);
			if (orbit.apoapsis <= orbit.Planet.SOI)
			{
				ApplyIteration();
				continue;
			}
			if (debug)
			{
				orbit.DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 1f, 0.05f));
			}
			Location location = orbit.GetLocation(orbit.orbitEndTime);
			Location location2 = orbit.Planet.GetLocation(orbit.orbitEndTime);
			Orbit orbit2 = new Orbit(location + location2, calculateTimeParameters: true, calculateEncounters: false);
			if (orbit2.direction == orbit.Planet.orbit.direction && !(orbit2.apoapsis >= orbit2.Planet.SOI))
			{
				if (debug)
				{
					orbit2.DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 1f, 0.05f));
				}
				array = new Orbit[2] { orbit, orbit2 };
				if (feedback(array))
				{
					ApplyIteration();
				}
				if (base.OutOfIterations)
				{
					break;
				}
			}
		}
		if (!valid(array))
		{
			return null;
		}
		return array;
	}
}
