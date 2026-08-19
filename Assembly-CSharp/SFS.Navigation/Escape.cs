using System;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.Navigation;

public static class Escape
{
	public static void Escape_Direct_Current(Location location, Orbit targetOrbit, Func<string> encounterText, bool drawEscapeText, double tolerance, ManeuverTree maneuvers)
	{
		Orbit[] array = Escape_Hohman_Current(location, targetOrbit, tolerance);
		if (array == null)
		{
			return;
		}
		Location location2 = location.planet.GetLocation(location.time);
		double angleRadians = location2.position.AngleRadians;
		bool below = targetOrbit.GetRadiusAtAngle(angleRadians) > location2.Radius;
		double targetStartTime = targetOrbit.GetLastAnglePassTime(location.time, angleRadians);
		Utility.FlybyTime flybyTime = new Utility.FlybyTime(array[1], targetOrbit, location.time, targetStartTime, firstPass: false);
		Utility.GetFloorRoof(crossing: false, below, targetOrbit, flybyTime, out var floor, out var roof);
		double escapeVelocity = location.planet.GetEscapeVelocity(location.Radius) + location.planet.parentBody.GetEscapeVelocity(location.planet.orbit.periapsis - location.planet.SOI);
		Double2 velocityNormal = location.velocity.normalized;
		int level;
		for (level = floor; level <= roof; level++)
		{
			new Iterator_Velocity().Calculate_Escape((double t) => new Location(location.time, location.planet, location.position, velocityNormal * Math_Utility.Lerp(10.0, escapeVelocity, t)), delegate(Orbit[] orbits)
			{
				if (Math.Abs(Intersection.GetMinHeightDiff(orbits[1], targetOrbit)) > tolerance)
				{
					return true;
				}
				Utility.FlybyTime flybyTime2 = new Utility.FlybyTime(orbits[1], targetOrbit, orbits[1].orbitStartTime, targetStartTime + targetOrbit.period * (double)level, firstPass: false);
				return (!below) ? (!flybyTime2.IsBeforeTarget) : flybyTime2.IsBeforeTarget;
			}, delegate(Orbit[] orbits)
			{
				Utility.FlybyTime flybyTimeData = new Utility.FlybyTime(orbits[1], targetOrbit, orbits[1].orbitStartTime, targetStartTime + targetOrbit.period * (double)level, firstPass: false);
				if (flybyTimeData.AbsoluteTimeDifference > targetOrbit.period * 0.001)
				{
					return false;
				}
				Double2 @double = orbits[0].GetVelocityAtAngle(location.position.AngleRadians) - location.velocity;
				maneuvers.AddManeuver(orbits[0], @double.magnitude, delegate(Color c)
				{
					orbits[0].DrawDashed(drawStats: false, drawStartText: false, drawEscapeText, c);
				}).AddManeuver(orbits[1], -1.0, delegate(Color c)
				{
					orbits[1].SetEncounter(null, flybyTimeData.flybyTime, encounterText);
					orbits[1].DrawDashed(drawStats: false, drawEscapeText, drawEndText: true, c);
				});
				return true;
			});
		}
	}

	public static void Escape_Direct_Future(Orbit fromOrbit, double time, Orbit targetOrbit, Func<string> encounterText, bool drawEscapeText, double ejectionAngleOffset, double tolerance, ManeuverTree maneuvers)
	{
		Orbit[] array = Escape_Hohman_Future(fromOrbit, time, targetOrbit, ejectionAngleOffset, tolerance, debug: false);
		if (array == null)
		{
			return;
		}
		Location location = fromOrbit.Planet.GetLocation(time);
		double angleRadians = location.position.AngleRadians;
		bool below = targetOrbit.GetRadiusAtAngle(angleRadians) > location.Radius;
		double angleRadians2 = ((!below) ? (-location.velocity) : location.velocity).AngleRadians;
		double targetStartTime = targetOrbit.GetLastAnglePassTime(time, angleRadians);
		Utility.FlybyTime flybyTime = new Utility.FlybyTime(array[1], targetOrbit, time, targetStartTime, firstPass: false);
		Utility.GetFloorRoof(crossing: false, below, targetOrbit, flybyTime, out var floor, out var roof);
		int level;
		for (level = floor; level <= roof; level++)
		{
			new Iterator_Orbit(fromOrbit, WorldTime.main.worldTime, angleRadians2 + ejectionAngleOffset).Calculate(delegate(Orbit[] orbits)
			{
				if (Math.Abs(Intersection.GetMinHeightDiff(orbits[1], targetOrbit)) > tolerance)
				{
					return true;
				}
				Utility.FlybyTime flybyTime2 = new Utility.FlybyTime(orbits[1], targetOrbit, orbits[1].orbitStartTime, targetStartTime + targetOrbit.period * (double)level, firstPass: false);
				return (!below) ? (!flybyTime2.IsBeforeTarget) : flybyTime2.IsBeforeTarget;
			}, delegate(Orbit[] orbits)
			{
				Utility.FlybyTime flybyTimeData = new Utility.FlybyTime(orbits[1], targetOrbit, orbits[1].orbitStartTime, targetStartTime + targetOrbit.period * (double)level, firstPass: false);
				if (flybyTimeData.AbsoluteTimeDifference > targetOrbit.period * 0.001)
				{
					return false;
				}
				double intersectionAngle = Intersection.GetIntersectionAngle(fromOrbit, orbits[0]);
				Double2 @double = orbits[0].GetVelocityAtAngle(intersectionAngle) - fromOrbit.GetVelocityAtAngle(intersectionAngle);
				maneuvers.AddManeuver(orbits[0], @double.magnitude, delegate(Color c)
				{
					orbits[0].DrawDashed(drawStats: false, drawStartText: false, drawEscapeText, c);
				}).AddManeuver(orbits[1], -1.0, delegate(Color c)
				{
					orbits[1].SetEncounter(null, flybyTimeData.flybyTime, encounterText);
					orbits[1].DrawDashed(drawStats: false, drawEscapeText, drawEndText: true, c);
				});
				return true;
			});
		}
	}

	public static Orbit[] Escape_Hohman_Current(Location location, Orbit targetOrbit, double tolerance, bool debug = false)
	{
		double escapeVelocity = location.planet.GetEscapeVelocity(location.Radius) + location.planet.parentBody.GetEscapeVelocity(location.planet.orbit.periapsis - location.planet.SOI);
		Double2 velocityNormal = location.velocity.normalized;
		return new Iterator_Velocity().Calculate_Escape((double t) => new Location(location.time, location.planet, location.position, velocityNormal * Math_Utility.Lerp(10.0, escapeVelocity, t)), Intersecting(targetOrbit, tolerance), Valid(targetOrbit, tolerance), debug);
	}

	public static Orbit[] Escape_Hohman_Future(Orbit fromOrbit, double time, Orbit targetOrbit, double ejectionAngleOffset, double tolerance, bool debug)
	{
		Location location = fromOrbit.Planet.GetLocation(time);
		double angleRadians = ((location.Radius > targetOrbit.GetRadiusAtAngle(location.position.AngleRadians)) ? (-location.velocity) : location.velocity).AngleRadians;
		return new Iterator_Orbit(fromOrbit, time, angleRadians + ejectionAngleOffset).Calculate(Intersecting(targetOrbit, tolerance), Valid(targetOrbit, tolerance), debug);
	}

	private static Iterator.Feedback<Orbit[]> Intersecting(Orbit targetOrbit, double tolerance)
	{
		return (Orbit[] orbits) => Math.Abs(Intersection.GetMinHeightDiff(orbits[1], targetOrbit)) > tolerance;
	}

	private static Iterator.Valid<Orbit[]> Valid(Orbit targetOrbit, double tolerance)
	{
		return (Orbit[] orbits) => Math.Abs(Intersection.GetMinHeightDiff(orbits[1], targetOrbit)) < tolerance * 1.05;
	}
}
