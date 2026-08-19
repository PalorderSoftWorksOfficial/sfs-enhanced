using System;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

public static class Kepler
{
	public const double Tau = Math.PI * 2.0;

	private const double Tolerance = 1E-07;

	private const int MaxIterations = 50;

	public static double GetAngleDiff(double from, double to, int direction)
	{
		double num = (to - from + Math.PI * 2.0 * (double)(-direction) * 10.0) % (Math.PI * 2.0);
		if (num != 0.0)
		{
			return num;
		}
		return Math.PI * 2.0 * (double)(-direction);
	}

	public static double ToTauRange(double a)
	{
		return (a + Math.PI * 20.0) % (Math.PI * 2.0);
	}

	public static double GetPeriapsis(double sma, double e)
	{
		return sma * (1.0 - e);
	}

	public static double GetApoapsis(double sma, double e)
	{
		if (!(e < 1.0))
		{
			return double.PositiveInfinity;
		}
		return sma * (1.0 + e);
	}

	public static double GetSemiMajorAxis(double peri, double e)
	{
		return peri / (1.0 - e);
	}

	public static double GetSemiMinorAxis(double sma, double e)
	{
		if (e < 1.0)
		{
			return sma * Math.Sqrt(1.0 - e * e);
		}
		if (e > 1.0)
		{
			return sma * Math.Sqrt(e * e - 1.0);
		}
		return sma;
	}

	public static double GetMass(double g, double r)
	{
		return g * (r * r);
	}

	public static double GetSemiLatusRectum(double p, double e)
	{
		return p * (e + 1.0);
	}

	public static double GetPeriapsisFromSemiLatusRectum(double l, double e)
	{
		return l / (e + 1.0);
	}

	public static double GetPeriod(double sma, double mass)
	{
		return Math.Sqrt(sma * sma * sma / mass) * (Math.PI * 2.0);
	}

	public static double GetMeanMotion(double sma, double mass)
	{
		sma = Math.Abs(sma);
		return Math.Sqrt(mass / (sma * sma * sma));
	}

	public static double GetTrueAnomalyFromEccentricAnomaly(double E, double e)
	{
		if (e < 1.0)
		{
			return 2.0 * Math.Atan2(Math.Sqrt(1.0 + e) * Math.Sin(E / 2.0), Math.Sqrt(1.0 - e) * Math.Cos(E / 2.0));
		}
		if (e > 1.0)
		{
			return 2.0 * Math.Atan2(Math.Sqrt(e + 1.0) * Math.Sinh(E / 2.0), Math.Sqrt(e - 1.0) * Math.Cosh(E / 2.0));
		}
		if (e == 1.0)
		{
			return 2.0 * Math.Atan(E);
		}
		return E;
	}

	public static double GetEccentricAnomalyFromTrueAnomaly(double v, double e)
	{
		if (e < 1.0)
		{
			return Math.Atan(Math.Tan(v / 2.0) / Math.Sqrt((1.0 + e) / (1.0 - e))) * 2.0;
		}
		if (e >= 1.0)
		{
			double num = (e + Math.Cos(v)) / (1.0 + e * Math.Cos(v));
			return Math.Log(num + Math.Sqrt(num * num - 1.0)) * (double)Math.Sign(NormalizeAngle(v));
		}
		return 0.0;
	}

	public static double GetRadiusAtAngle(this Orbit orbit, double angleRadians)
	{
		return orbit.slr / (1.0 + orbit.ecc * Math.Cos(angleRadians - orbit.arg));
	}

	public static double GetRadiusAtTrueAnomaly(double l, double e, double v)
	{
		return l / (1.0 + e * Math.Cos(v));
	}

	public static double GetTrueAnomalyAtRadius(double r, double l, double e)
	{
		double num = Math.Acos((l / r - 1.0) / e);
		if (double.IsNaN(num))
		{
			Debug.Log("Fixed");
			return 0.0;
		}
		return num;
	}

	public static double GetTrueAnomalyAtRadius(this Orbit orbit, double r)
	{
		return Math.Acos((orbit.slr / r - 1.0) / orbit.ecc);
	}

	public static Double2 GetVelocity(double sma, double r, double n, double E, double e, double arg, int direction)
	{
		return GetVelocityNormal(E, e, arg) * (sma * sma * n * (double)direction / r);
	}

	public static Double2 GetVelocityNormal(double E, double e, double arg)
	{
		if (e < 1.0)
		{
			return new Double2(0.0 - Math.Sin(E), Math.Cos(E) * Math.Sqrt(1.0 - e * e)).Rotate(arg);
		}
		return new Double2(0.0 - Math.Sinh(E), Math.Cosh(E) * Math.Sqrt(e * e - 1.0)).Rotate(arg);
	}

	public static double GetMeanAnomaly(double e, double v)
	{
		if (e < 1.0)
		{
			double num = 2.0 * Math.Atan(Math.Tan(v / 2.0) / Math.Sqrt((1.0 + e) / (1.0 - e)));
			return num - e * Math.Sin(num);
		}
		if (e > 1.0)
		{
			double num2 = (e + Math.Cos(v)) / (1.0 + e * Math.Cos(v));
			double num3 = Math.Log(num2 + Math.Sqrt(num2 * num2 - 1.0));
			return (e * Math.Sinh(num3) - num3) * (double)Math.Sign(NormalizeAngle(v));
		}
		return 0.0;
	}

	public static double GetTimeToPeriapsis(double r, double e, double l, double meanMotion)
	{
		return GetTimeToPeriapsis(GetTrueAnomalyAtRadius(r, l, e), e, meanMotion, 1);
	}

	public static double GetTimeToPeriapsis(double trueAnomaly, double e, double meanMotion, int direction)
	{
		return GetMeanAnomaly(e, NormalizeAngle(trueAnomaly)) / meanMotion * (double)direction;
	}

	public static double GetEccentricAnomaly(double M, double e)
	{
		if (e >= 1.0)
		{
			return GetEccentricAnomalyHyperbolic(M, e);
		}
		if (e > 0.8)
		{
			return GetEccentricAnomalyExtremeEccentricity(M, e);
		}
		return GetEccentricAnomalyElliptical(M, e);
	}

	private static double GetEccentricAnomalyElliptical(double M, double e)
	{
		double num = 1.0;
		double num2 = M + e * Math.Sin(M) + 0.5 * e * e * Math.Sin(2.0 * M);
		while (Math.Abs(num) > 1E-07)
		{
			double num3 = num2 - e * Math.Sin(num2);
			num = (M - num3) / (1.0 - e * Math.Cos(num2));
			num2 += num;
		}
		return num2;
	}

	private static double GetEccentricAnomalyExtremeEccentricity(double M, double e)
	{
		double num = M + 0.85 * e * (double)Math.Sign(Math.Sin(M));
		for (int i = 0; i < 50; i++)
		{
			double num2 = e * Math.Sin(num);
			double num3 = e * Math.Cos(num);
			double num4 = num - num2 - M;
			double num5 = 1.0 - num3;
			num += -5.0 * num4 / (num5 + (double)Math.Sign(num5) * Math.Sqrt(Math.Abs(16.0 * num5 * num5 - 20.0 * num4 * num2)));
		}
		return num;
	}

	private static double GetEccentricAnomalyHyperbolic(double M, double e)
	{
		if (double.IsInfinity(M))
		{
			return M;
		}
		double num = 1.0;
		double num2 = 2.0 * M / e;
		double num3 = Math.Log(Math.Sqrt(num2 * num2 + 1.0) + num2);
		while (Math.Abs(num) > 1E-07)
		{
			num = (e * Math.Sinh(num3) - num3 - M) / (e * Math.Cosh(num3) - 1.0);
			num3 -= num;
		}
		return num3;
	}

	public static double GetSphereOfInfluence(double sma, double mass, double parentMass, double multiplier)
	{
		return sma * Math.Pow(mass / parentMass, 0.4) * multiplier;
	}

	public static Double2 GetPosition(double r, double trueAnomaly, double arg)
	{
		return new Double2(Math.Cos(trueAnomaly + arg) * r, Math.Sin(trueAnomaly + arg) * r);
	}

	public static double GetEscapeVelocity(Planet planet, double radius)
	{
		return Math.Sqrt(2.0 * planet.mass / radius);
	}

	public static double NormalizeAngle(double angle)
	{
		while (angle > Math.PI)
		{
			angle -= Math.PI * 2.0;
		}
		while (angle < -Math.PI)
		{
			angle += Math.PI * 2.0;
		}
		return angle;
	}

	public static double PositiveAngle(double angle)
	{
		while (angle > Math.PI * 2.0)
		{
			angle -= Math.PI * 2.0;
		}
		while (angle < 0.0)
		{
			angle += Math.PI * 2.0;
		}
		return angle;
	}
}
