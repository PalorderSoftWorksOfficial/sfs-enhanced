using System;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

[Serializable]
public class Orbit : I_Path
{
	private const double StationaryThreshold = 0.1;

	private const double Margin = 0.1;

	public double sma;

	public double semiMinorAxis;

	public double slr;

	public double arg;

	public Matrix2x2_Double arg_Matrix;

	public double periapsis;

	public double apoapsis;

	public double ecc;

	public double meanMotion;

	public int direction;

	public double period;

	public double periapsisPassageTime;

	[Space]
	public PathType pathType;

	private Planet nextPlanet;

	[Space]
	public double orbitStartTime;

	public double orbitEndTime;

	[Space]
	public Func<string> encounterText;

	private Location location_Out;

	private double trueAnomaly_Out;

	public PathType PathType => pathType;

	public double PathStartTime => orbitStartTime;

	public double PathEndTime => orbitEndTime;

	public Planet Planet => location_Out.planet;

	public Planet NextPlanet => nextPlanet;

	public static Orbit TryCreateOrbit(Location location, bool calculateTimeParameters, bool calculateEncounters, out bool success)
	{
		success = false;
		if (location.velocity.Mag_LessThan(0.1))
		{
			return null;
		}
		if (double.IsNaN(Double3.Cross(location.position, location.velocity).z))
		{
			return null;
		}
		if (location.position.magnitude > location.planet.SOI * 1.01)
		{
			return null;
		}
		Orbit orbit = new Orbit(location, calculateTimeParameters, calculateEncounters);
		if (double.IsNaN(orbit.meanMotion))
		{
			return null;
		}
		if (orbit.ecc == 1.0)
		{
			return null;
		}
		if (double.IsNaN(orbit.periapsisPassageTime))
		{
			return null;
		}
		success = true;
		return orbit;
	}

	public Orbit(Location location, bool calculateTimeParameters, bool calculateEncounters)
	{
		orbitStartTime = location.time;
		Double3 b = Double3.Cross(location.position, location.velocity);
		Double2 @double = (Double2)(Double3.Cross((Double3)location.velocity, b) / location.planet.mass) - location.position.normalized;
		ecc = @double.magnitude;
		sma = location.planet.mass / (0.0 - 2.0 * (Math.Pow(location.velocity.magnitude, 2.0) / 2.0 - location.planet.mass / location.Radius));
		semiMinorAxis = Kepler.GetSemiMinorAxis(sma, ecc);
		periapsis = Kepler.GetPeriapsis(sma, ecc);
		apoapsis = Kepler.GetApoapsis(sma, ecc);
		arg = @double.AngleRadians;
		arg_Matrix = Matrix2x2_Double.Angle(arg);
		slr = Kepler.GetSemiLatusRectum(periapsis, ecc);
		direction = Math.Sign(b.z);
		bool flag = apoapsis >= location.planet.SOI;
		trueAnomaly_Out = Kepler.NormalizeAngle(location.position.AngleRadians - arg);
		location_Out = location;
		if (calculateTimeParameters)
		{
			period = (flag ? 0.0 : Kepler.GetPeriod(sma, location.planet.mass));
			meanMotion = Kepler.GetMeanMotion(sma, location.planet.mass);
			if (double.IsNaN(meanMotion))
			{
				Debug.Log("MM is NaN");
			}
			double num = Kepler.GetMeanAnomaly(ecc, trueAnomaly_Out) / meanMotion * (double)direction;
			periapsisPassageTime = orbitStartTime - num - period * 10.0;
		}
		if (flag)
		{
			SetOrbitType(orbitEndTime: (location.planet.SOI == double.PositiveInfinity) ? double.PositiveInfinity : (periapsisPassageTime + Kepler.GetTimeToPeriapsis(location.planet.SOI, ecc, slr, meanMotion)), orbitType: PathType.Escape, nextPlanet: location.planet.parentBody);
		}
		else
		{
			SetOrbitType(PathType.Eternal, null, double.PositiveInfinity);
		}
		if (calculateEncounters)
		{
			FindEncounters(location.time, flag ? double.PositiveInfinity : (location.time + period * 0.99));
		}
	}

	public Orbit(double sma, double ecc, double arg, int direction, Planet planet, PathType pathType, Planet nextPlanet)
	{
		location_Out = new Location(planet, Double2.zero, Double2.zero);
		this.ecc = ecc;
		this.arg = arg;
		this.direction = direction;
		this.sma = sma;
		semiMinorAxis = Kepler.GetSemiMinorAxis(sma, ecc);
		periapsis = Kepler.GetPeriapsis(sma, ecc);
		apoapsis = Kepler.GetApoapsis(sma, ecc);
		slr = Kepler.GetSemiLatusRectum(periapsis, ecc);
		arg_Matrix = Matrix2x2_Double.Angle(arg);
		period = Kepler.GetPeriod(sma, planet.mass);
		meanMotion = Kepler.GetMeanMotion(sma, planet.mass);
		this.pathType = pathType;
		this.nextPlanet = nextPlanet;
		if (pathType == PathType.Eternal)
		{
			orbitEndTime = double.PositiveInfinity;
		}
	}

	private void SetOrbitType(PathType orbitType, Planet nextPlanet, double orbitEndTime)
	{
		pathType = orbitType;
		this.nextPlanet = nextPlanet;
		this.orbitEndTime = orbitEndTime;
	}

	public void SetEncounter(Planet nextPlanet, double orbitEndTime, Func<string> encounterText)
	{
		SetOrbitType(PathType.Encounter, nextPlanet, orbitEndTime);
		this.encounterText = encounterText;
	}

	public Vector3[] GetPoints(double fromTrueAnomaly, double toTrueAnomaly, int resolution, double scaleMultiplier)
	{
		if (ecc < 1.0)
		{
			return GetPoints_Ellipse(fromTrueAnomaly, toTrueAnomaly, resolution, scaleMultiplier);
		}
		double eccentricAnomalyFromTrueAnomaly = Kepler.GetEccentricAnomalyFromTrueAnomaly(fromTrueAnomaly, ecc);
		double eccentricAnomalyFromTrueAnomaly2 = Kepler.GetEccentricAnomalyFromTrueAnomaly(toTrueAnomaly, ecc);
		double num = ((ecc < 1.0) ? Kepler.GetAngleDiff(eccentricAnomalyFromTrueAnomaly, eccentricAnomalyFromTrueAnomaly2, -direction) : (eccentricAnomalyFromTrueAnomaly2 - eccentricAnomalyFromTrueAnomaly)) / (double)resolution;
		Vector3[] array = new Vector3[resolution + 1];
		for (int i = 0; i < resolution + 1; i++)
		{
			array[i] = GetPositionFromEccentricAnomaly(eccentricAnomalyFromTrueAnomaly + num * (double)i) * scaleMultiplier;
		}
		return array;
	}

	public Vector3[] GetPoints_Ellipse(double fromTrueAnomaly, double toTrueAnomaly, int resolution, double scaleMultiplier)
	{
		double num = Kepler.NormalizeAngle(fromTrueAnomaly);
		double num2 = Kepler.NormalizeAngle(toTrueAnomaly);
		bool flag = fromTrueAnomaly == toTrueAnomaly;
		Double2[] ellipseArray = GetEllipseArray(resolution);
		int num3;
		int num5;
		int num6;
		if (direction > 0)
		{
			num3 = -(ellipseArray.Length - 1);
			double num4 = 0.0 - ellipseArray[Math.Abs(num3)].y;
			while (num4 < num && num3 < ellipseArray.Length - 1)
			{
				num3++;
				num4 = (double)Math.Sign(num3) * ellipseArray[Math.Abs(num3)].y;
			}
			if (flag)
			{
				num5 = num3;
				num6 = 2 * ellipseArray.Length + 1;
			}
			else
			{
				num5 = -(ellipseArray.Length - 1);
				num4 = 0.0 - ellipseArray[Math.Abs(num5)].y;
				while (num4 < num2 && num5 < ellipseArray.Length - 1)
				{
					num5++;
					num4 = (double)Math.Sign(num5) * ellipseArray[Math.Abs(num5)].y;
				}
				num6 = num5 - num3;
				if (num6 < 0)
				{
					num6 += 2 * ellipseArray.Length - 1;
				}
				num6 += 2;
			}
		}
		else
		{
			num3 = ellipseArray.Length - 1;
			double num4 = ellipseArray[Math.Abs(num3)].y;
			while (num4 > num && num3 > -(ellipseArray.Length - 1))
			{
				num3--;
				num4 = (double)Math.Sign(num3) * ellipseArray[Math.Abs(num3)].y;
			}
			if (flag)
			{
				num5 = num3;
				num6 = 2 * ellipseArray.Length + 1;
			}
			else
			{
				num5 = ellipseArray.Length - 1;
				num4 = ellipseArray[Math.Abs(num5)].y;
				while (num4 > num2 && num5 > -(ellipseArray.Length - 1))
				{
					num5--;
					num4 = (double)Math.Sign(num5) * ellipseArray[Math.Abs(num5)].y;
				}
				num6 = num3 - num5;
				if (num6 < 0)
				{
					num6 += 2 * ellipseArray.Length - 1;
				}
				num6 += 2;
			}
		}
		Vector3[] array = new Vector3[num6];
		int num7 = 0;
		double radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, num);
		array[num7] = Kepler.GetPosition(radiusAtTrueAnomaly, num, arg) * scaleMultiplier;
		num7++;
		bool flag2 = flag;
		int num8 = num3;
		while ((num8 != num5) | flag2)
		{
			flag2 = false;
			double num4 = (double)Math.Sign(num8) * ellipseArray[Math.Abs(num8)].y;
			radiusAtTrueAnomaly = ellipseArray[Math.Abs(num8)].x;
			array[num7] = Kepler.GetPosition(radiusAtTrueAnomaly, num4, arg) * scaleMultiplier;
			num7++;
			if (direction > 0)
			{
				num8++;
				if (num8 == ellipseArray.Length)
				{
					num8 = -(ellipseArray.Length - 1);
				}
			}
			else
			{
				num8--;
				if (num8 == -ellipseArray.Length)
				{
					num8 = ellipseArray.Length - 1;
				}
			}
		}
		radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, num2);
		array[num7] = Kepler.GetPosition(radiusAtTrueAnomaly, num2, arg) * scaleMultiplier;
		return array;
	}

	public Double2[] GetEllipseArray(int resolution)
	{
		double num = Math.PI * 2.0 / (double)resolution;
		Double2[] array = new Double2[resolution];
		uint num2 = 0u;
		double num3 = Math.PI;
		double radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, num3);
		array[num2] = new Double2(radiusAtTrueAnomaly, num3);
		num2++;
		num3 -= Math.Max(2.0 - radiusAtTrueAnomaly / sma, 1E-06) * num;
		while (num3 > 0.0 && num2 < resolution - 1)
		{
			radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, num3);
			array[num2] = new Double2(radiusAtTrueAnomaly, num3);
			num2++;
			num3 -= Math.Max(2.0 - radiusAtTrueAnomaly / sma, 1E-06) * num;
		}
		array[num2] = new Double2(periapsis, 0.0);
		num2++;
		Double2[] array2 = new Double2[num2];
		for (uint num4 = 0u; num4 < num2; num4++)
		{
			array2[num4] = array[num2 - num4 - 1];
		}
		return array2;
	}

	public Double2 GetPositionFromEccentricAnomaly(double eccentricAnomaly)
	{
		if (ecc < 1.0)
		{
			return arg_Matrix.Apply(new Double2((Math.Cos(eccentricAnomaly) - ecc) * sma, Math.Sin(eccentricAnomaly) * semiMinorAxis));
		}
		return arg_Matrix.Apply(new Double2((ecc - Math.Cosh(eccentricAnomaly)) * (0.0 - sma), Math.Sinh(eccentricAnomaly) * (0.0 - semiMinorAxis)));
	}

	public Double2 GetPositionAtAngle(double angleRadians)
	{
		return GetPositionAtTrueAnomaly(angleRadians - arg);
	}

	public Double2 GetPositionAtTrueAnomaly(double trueAnomaly)
	{
		return Kepler.GetPosition(Kepler.GetRadiusAtTrueAnomaly(slr, ecc, trueAnomaly), trueAnomaly, arg);
	}

	public Double2 GetVelocityAtAngle(double angleRadians)
	{
		return GetVelocityAtTrueAnomaly(angleRadians - arg);
	}

	public Double2 GetVelocityAtTrueAnomaly(double trueAnomaly)
	{
		double radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, trueAnomaly);
		double eccentricAnomalyFromTrueAnomaly = Kepler.GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, ecc);
		return Kepler.GetVelocity(sma, radiusAtTrueAnomaly, meanMotion, eccentricAnomalyFromTrueAnomaly, ecc, arg, direction);
	}

	public Location GetLocation(double time)
	{
		UpdateLocation(time);
		return location_Out;
	}

	public double GetStopTimewarpTime(double timeOld, double timeNew)
	{
		double timewarpRadius_Descend = location_Out.planet.TimewarpRadius_Descend;
		if (apoapsis < timewarpRadius_Descend + 0.1)
		{
			if (apoapsis > location_Out.planet.TimewarpRadius_Ascend)
			{
				double nextTrueAnomalyPassTime = GetNextTrueAnomalyPassTime(timeOld, Math.PI);
				if (nextTrueAnomalyPassTime > timeOld && nextTrueAnomalyPassTime < timeNew)
				{
					return nextTrueAnomalyPassTime;
				}
			}
			return double.PositiveInfinity;
		}
		if (periapsis > timewarpRadius_Descend - 0.1)
		{
			return double.PositiveInfinity;
		}
		double nextTrueAnomalyPassTime2 = GetNextTrueAnomalyPassTime(timeOld, Kepler.GetTrueAnomalyAtRadius(timewarpRadius_Descend, slr, ecc) * (double)(-direction));
		if (nextTrueAnomalyPassTime2 > timeOld && nextTrueAnomalyPassTime2 < timeNew)
		{
			return nextTrueAnomalyPassTime2;
		}
		return double.PositiveInfinity;
	}

	public double GetTrueAnomaly(double time)
	{
		UpdateLocation(time);
		return trueAnomaly_Out;
	}

	private void UpdateLocation(double newTime)
	{
		if (location_Out.time != newTime && !double.IsNaN(newTime) && !double.IsNaN(meanMotion))
		{
			double eccentricAnomaly = Kepler.GetEccentricAnomaly((newTime - periapsisPassageTime) * meanMotion * (double)direction, ecc);
			double trueAnomalyFromEccentricAnomaly = Kepler.GetTrueAnomalyFromEccentricAnomaly(eccentricAnomaly, ecc);
			double radiusAtTrueAnomaly = Kepler.GetRadiusAtTrueAnomaly(slr, ecc, trueAnomalyFromEccentricAnomaly);
			Double2 position = Kepler.GetPosition(radiusAtTrueAnomaly, trueAnomalyFromEccentricAnomaly, arg);
			Double2 velocity = Kepler.GetVelocity(sma, radiusAtTrueAnomaly, meanMotion, eccentricAnomaly, ecc, arg, direction);
			Location location = new Location(newTime, location_Out.planet, position, velocity);
			if (!double.IsNaN(trueAnomaly_Out) && !double.IsNaN(location.position.x) && !double.IsNaN(location.position.y) && !double.IsNaN(location.velocity.x) && !double.IsNaN(location.velocity.y))
			{
				location_Out = location;
				trueAnomaly_Out = trueAnomalyFromEccentricAnomaly;
			}
		}
	}

	public double GetNextAnglePassTime(double time, double angleRadians)
	{
		return GetLastAnglePassTime(time, angleRadians) + period;
	}

	public double GetLastAnglePassTime(double time, double angleRadians)
	{
		double trueAnomaly = angleRadians - arg;
		return GetLastTrueAnomalyPassTime(time, trueAnomaly);
	}

	public double GetNextTrueAnomalyPassTime(double time, double trueAnomaly)
	{
		return GetLastTrueAnomalyPassTime(time, trueAnomaly) + period;
	}

	public double GetLastTrueAnomalyPassTime(double time, double trueAnomaly)
	{
		double num = periapsisPassageTime + Kepler.GetTimeToPeriapsis(trueAnomaly, ecc, meanMotion, direction);
		if (pathType == PathType.Escape)
		{
			return num;
		}
		return num + (double)Math_Utility.GetFitsTime(period, num, time) * period;
	}

	public bool UpdateEncounters()
	{
		if (pathType != PathType.Eternal)
		{
			return false;
		}
		double num = Math.Max(orbitStartTime, WorldTime.main.worldTime);
		double window_End = num + period * 0.9;
		return FindEncounters(num, window_End);
	}

	private bool FindEncounters(double window_Start, double window_End)
	{
		bool result = false;
		Planet[] satellites = location_Out.planet.satellites;
		foreach (Planet planet in satellites)
		{
			if (apoapsis > planet.orbit.periapsis - planet.SOI + 0.1 && periapsis < planet.orbit.apoapsis + planet.SOI - 0.1 && ProcessEncounters(planet, window_Start, Math.Min(window_End, orbitEndTime)))
			{
				result = true;
			}
		}
		return result;
	}

	private bool ProcessEncounters(Planet satellite, double window_Start, double window_End)
	{
		double maxAcceleration = location_Out.planet.GetGravity(satellite.orbit.periapsis - satellite.SOI) * 2.0;
		double time = window_Start;
		for (int i = 0; i < 100; i++)
		{
			if (time >= window_End)
			{
				return false;
			}
			if (GetFastestPossibleArrivalTime(ref time, satellite, maxAcceleration))
			{
				SetEncounter(satellite, time, () => Loc.main.Encounter);
				return true;
			}
		}
		return false;
	}

	private bool GetFastestPossibleArrivalTime(ref double time, Planet satellite, double maxAcceleration)
	{
		Double2 position = GetLocation(time).position;
		if (position.Mag_LessThan(satellite.orbit.periapsis - (satellite.SOI + 0.1)))
		{
			double num = GetNextTrueAnomalyPassTime(time, this.GetTrueAnomalyAtRadius(satellite.orbit.periapsis - satellite.SOI) * (double)direction);
			if (num == time)
			{
				num++;
			}
			time = num;
			return false;
		}
		if (position.Mag_MoreThan(satellite.orbit.apoapsis + satellite.SOI + 0.1))
		{
			if (period == 0.0 && time > periapsisPassageTime)
			{
				time = double.PositiveInfinity;
				return false;
			}
			double num2 = GetNextTrueAnomalyPassTime(time, this.GetTrueAnomalyAtRadius(satellite.orbit.apoapsis + satellite.SOI) * (double)(-direction));
			if (num2 == time)
			{
				num2++;
			}
			time = num2;
			return false;
		}
		Double2 @double = satellite.GetLocation(time).position - position;
		double x = (satellite.GetLocation(time).velocity - GetLocation(time).velocity).Rotate(0.0 - @double.AngleRadians).x;
		double num3 = @double.magnitude - satellite.SOI;
		if (num3 < 0.1 && x < 0.0)
		{
			return true;
		}
		double num4 = time + GetFallTime(x, num3, maxAcceleration);
		if (periapsis < satellite.orbit.periapsis - (satellite.SOI + 0.1))
		{
			double nextTrueAnomalyPassTime = GetNextTrueAnomalyPassTime(time, this.GetTrueAnomalyAtRadius(satellite.orbit.periapsis - satellite.SOI) * (double)direction);
			if (nextTrueAnomalyPassTime - time > 0.1 && nextTrueAnomalyPassTime < num4)
			{
				num4 = nextTrueAnomalyPassTime;
			}
		}
		time = num4;
		return false;
	}

	private double GetFallTime(double verticalVelocity, double startHeight, double gravity)
	{
		double num = Math.Sqrt((startHeight + Math.Pow(verticalVelocity, 2.0) / (gravity * 2.0)) * 2.0 / gravity);
		double num2 = verticalVelocity / gravity;
		return num + num2;
	}
}
