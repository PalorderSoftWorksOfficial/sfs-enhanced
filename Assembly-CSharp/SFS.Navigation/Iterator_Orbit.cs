using System;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.Navigation;

public class Iterator_Orbit : Iterator
{
	private readonly Orbit orbit;

	private readonly double time;

	private readonly double velocityAngle;

	public Iterator_Orbit(Orbit orbit, double time, double velocityAngle)
		: base(35)
	{
		this.orbit = orbit;
		this.time = time;
		this.velocityAngle = velocityAngle;
	}

	public Orbit[] Calculate(Feedback<Orbit[]> feedback, Valid<Orbit[]> valid, bool debug = false)
	{
		Orbit[] output;
		while (true)
		{
			if (base.OutOfIterations)
			{
				return null;
			}
			iterationCount++;
			double num = velocityAngle + (1.0 - base.Current) * Math.PI * 0.5 * (double)(-this.orbit.direction);
			Orbit orbit = GetOrbit(num);
			if (debug)
			{
				orbit?.DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 0f, 0.05f));
			}
			if (GetAfterEscapeOrbit(orbit, num, out output))
			{
				if (debug)
				{
					output[1].DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, new Color(0f, 1f, 0f, 0.05f));
				}
				if (feedback(output))
				{
					ApplyIteration();
				}
				if (base.OutOfIterations)
				{
					break;
				}
			}
		}
		if (!valid(output))
		{
			return null;
		}
		return output;
	}

	private Orbit GetOrbit(double exitPositionAngle)
	{
		double num = exitPositionAngle - velocityAngle + Math.PI / 2.0;
		double num2 = this.orbit.Planet.SOI * Math.Cos(num);
		if (num2 == 0.0)
		{
			return null;
		}
		double num3 = 1.0 / (num2 * num2) - 2.0 * this.orbit.ecc / (this.orbit.slr * num2) * Math.Sin(velocityAngle - this.orbit.arg) - (1.0 - this.orbit.ecc * this.orbit.ecc) / (this.orbit.slr * this.orbit.slr);
		double num4 = 2.0 * (1.0 / this.orbit.Planet.SOI - 1.0 / this.orbit.slr - this.orbit.ecc / this.orbit.slr * Math.Cos(exitPositionAngle - this.orbit.arg));
		if (num3 == 0.0 || num4 == 0.0)
		{
			return null;
		}
		double num5 = num4 / num3;
		double num6 = Math.Sqrt(Math.Pow(1.0 / this.orbit.Planet.SOI - 1.0 / num5, 2.0) + Math.Pow(Math.Tan(num) / this.orbit.Planet.SOI, 2.0)) * num5;
		double arg = ((num6 > 0.0) ? (exitPositionAngle + Math.Acos((num5 / this.orbit.Planet.SOI - 1.0) / num6) * (double)(-this.orbit.direction)) : 0.0);
		if (num6 < 0.0)
		{
			return null;
		}
		Orbit orbit = new Orbit(Kepler.GetSemiMajorAxis(Kepler.GetPeriapsisFromSemiLatusRectum(num5, num6), num6), num6, arg, this.orbit.direction, this.orbit.Planet, PathType.Escape, this.orbit.Planet.parentBody);
		ApplyEscapeTime(time, Intersection.GetIntersectionAngle(this.orbit, orbit), orbit);
		return orbit;
	}

	private static bool GetAfterEscapeOrbit(Orbit A, double positionAngle, out Orbit[] output)
	{
		if (A == null)
		{
			output = null;
			return false;
		}
		Location location = A.Planet.orbit.GetLocation(A.orbitEndTime);
		Orbit orbit = new Orbit(new Location(A.orbitEndTime, A.Planet.parentBody, location.position + Double2.CosSin(positionAngle, A.Planet.SOI), location.velocity + A.GetVelocityAtAngle(positionAngle)), calculateTimeParameters: true, calculateEncounters: false);
		if (orbit.direction != A.Planet.orbit.direction || orbit.apoapsis >= orbit.Planet.SOI)
		{
			output = null;
			return false;
		}
		output = new Orbit[2] { A, orbit };
		return true;
	}

	private static void ApplyEscapeTime(double time, double startAngle, Orbit orbit)
	{
		double timeToPeriapsis = Kepler.GetTimeToPeriapsis(startAngle - orbit.arg, orbit.ecc, orbit.meanMotion, orbit.direction);
		double timeToPeriapsis2 = Kepler.GetTimeToPeriapsis(orbit.Planet.SOI, orbit.ecc, orbit.slr, orbit.meanMotion);
		orbit.orbitStartTime = time;
		orbit.periapsisPassageTime = time - timeToPeriapsis;
		orbit.orbitEndTime = time - timeToPeriapsis + timeToPeriapsis2;
	}
}
