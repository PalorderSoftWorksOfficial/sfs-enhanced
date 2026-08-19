using System;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.Navigation;

public static class Basic
{
	public static void Direct_Current(Location location, Orbit fromOrbit, Orbit targetOrbit, Func<string> encounterText, double tolerance, ManeuverTree maneuvers)
	{
		Double2 velocityNormal = location.velocity.normalized;
		double escapeVelocity = location.planet.GetEscapeVelocity(location.Radius);
		double targetStartTime = targetOrbit.GetLastAnglePassTime(location.time, location.position.AngleRadians);
		Orbit hohmanTransfer = GetHohmanTransfer(location, fromOrbit, targetOrbit, tolerance, out var crossing);
		bool below = targetOrbit.GetRadiusAtAngle(location.position.AngleRadians) > location.Radius;
		if (hohmanTransfer == null)
		{
			return;
		}
		Utility.FlybyTime flybyTime = new Utility.FlybyTime(hohmanTransfer, targetOrbit, location.time, targetStartTime, firstPass: false);
		Utility.GetFloorRoof(crossing, below, targetOrbit, flybyTime, out var floor, out var roof);
		int level;
		for (level = floor; level <= roof; level++)
		{
			new Iterator_Velocity().Calculate((double t) => new Location(location.time, location.planet, location.position, velocityNormal * Math_Utility.Lerp(10.0, escapeVelocity, t)), delegate(Orbit orbit)
			{
				double minHeightDiff = Intersection.GetMinHeightDiff(orbit, targetOrbit);
				return (Math.Abs(minHeightDiff) > tolerance) ? (minHeightDiff < 0.0) : new Utility.FlybyTime(orbit, targetOrbit, location.time, targetStartTime + targetOrbit.period * (double)level, firstPass: false).IsBeforeTarget;
			}, delegate(Orbit orbit)
			{
				if (Math.Abs(Intersection.GetMinHeightDiff(orbit, targetOrbit)) > tolerance)
				{
					return false;
				}
				Utility.FlybyTime flybyTimeData = new Utility.FlybyTime(orbit, targetOrbit, location.time, targetStartTime + targetOrbit.period * (double)level, firstPass: false);
				if (flybyTimeData.AbsoluteTimeDifference > targetOrbit.period * 0.001)
				{
					return false;
				}
				Double2 @double = orbit.GetVelocityAtAngle(location.position.AngleRadians) - location.velocity;
				maneuvers.AddManeuver(orbit, @double.magnitude, delegate(Color c)
				{
					orbit.SetEncounter(null, flybyTimeData.flybyTime, encounterText);
					orbit.DrawDashed(drawStats: false, drawStartText: false, drawEndText: true, c);
				});
				return true;
			});
		}
	}

	public static Orbit GetHohmanTransfer(Location location, Orbit fromOrbit, Orbit targetOrbit, double tolerance, out bool crossing)
	{
		crossing = Intersection.Intersects(fromOrbit, targetOrbit);
		if (crossing)
		{
			return fromOrbit;
		}
		return GetHohmanTransfer(location, targetOrbit, tolerance);
	}

	public static Orbit GetHohmanTransfer(Location location, Orbit targetOrbit, double tolerance)
	{
		Double2 velocityNormal = location.velocity.normalized;
		double escapeVelocity = Math.Sqrt(2.0 * location.planet.mass / location.Radius);
		bool below = location.Radius < targetOrbit.GetRadiusAtAngle(location.position.AngleRadians);
		return new Iterator_Velocity().Calculate((double t) => new Location(location.time, location.planet, location.position, velocityNormal * Math_Utility.Lerp(10.0, escapeVelocity, t)), (Orbit orbit) => Math.Abs(Intersection.GetMinHeightDiff(orbit, targetOrbit)) > tolerance == below, (Orbit orbit) => Math.Abs(Intersection.GetMinHeightDiff(orbit, targetOrbit)) < tolerance * 1.05);
	}

	public static void GetPredictedHohman(Location location, Orbit fromOrbit, Orbit targetOrbit, double tolerance, out Orbit predicted, out Location departureLocation)
	{
		Orbit hohmanTransfer = GetHohmanTransfer(location, targetOrbit, tolerance);
		if (hohmanTransfer == null)
		{
			predicted = null;
			departureLocation = null;
		}
		else
		{
			double angleRadians = location.position.AngleRadians + GetArrivalDiff(location, hohmanTransfer, targetOrbit);
			departureLocation = new Location(location.time, fromOrbit.Planet, fromOrbit.GetPositionAtAngle(angleRadians), fromOrbit.GetVelocityAtAngle(angleRadians));
			predicted = GetHohmanTransfer(departureLocation, targetOrbit, tolerance);
		}
	}

	public static double GetArrivalDiff(Location location, Orbit hohman, Orbit targetOrbit)
	{
		double intersectionAngle = Intersection.GetIntersectionAngle(hohman, targetOrbit);
		double nextAnglePassTime = hohman.GetNextAnglePassTime(location.time, intersectionAngle);
		return targetOrbit.GetTrueAnomaly(nextAnglePassTime) + targetOrbit.arg - intersectionAngle;
	}
}
