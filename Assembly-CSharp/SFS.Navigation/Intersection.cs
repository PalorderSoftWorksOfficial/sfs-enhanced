using System;
using SFS.World;

namespace SFS.Navigation;

public static class Intersection
{
	public static double GetHeightDifferenceAtAngle(Orbit A, Orbit B, double angle)
	{
		return A.GetRadiusAtAngle(angle) - B.GetRadiusAtAngle(angle);
	}

	public static double GetMinHeightDiff(Orbit A, Orbit B)
	{
		Intersects(A, B, out var difference);
		return difference;
	}

	private static void Intersects(Orbit A, Orbit B, out double difference)
	{
		if (GetIntersectionAngles(B, A, out var angle_A, out var _))
		{
			difference = 0.0;
		}
		else
		{
			difference = GetHeightDifferenceAtAngle(A, B, angle_A);
		}
	}

	public static bool Intersects(Orbit A, Orbit B)
	{
		double num = Math.Cos(Kepler.NormalizeAngle(B.arg - A.arg));
		double num2 = Math.Sqrt(Math.Pow(A.ecc / A.slr, 2.0) + Math.Pow(B.ecc / B.slr, 2.0) - 2.0 * A.ecc * B.ecc * num / (A.slr * B.slr));
		double value = 1.0 / A.slr - 1.0 / B.slr;
		return num2 > Math.Abs(value);
	}

	public static bool GetIntersectionAngles(Orbit A, Orbit B, out double angle_A, out double angle_B)
	{
		double num = Kepler.NormalizeAngle(B.arg - A.arg);
		double num2 = Math.Cos(num);
		double num3 = Math.Sqrt(Math.Pow(A.ecc / A.slr, 2.0) + Math.Pow(B.ecc / B.slr, 2.0) - 2.0 * A.ecc * B.ecc * num2 / (A.slr * B.slr));
		double num4 = 1.0 / A.slr - 1.0 / B.slr;
		double num5 = Math.Acos((B.ecc * num2 / B.slr - A.ecc / A.slr) / num3);
		if (Math.Sin(num) < 0.0)
		{
			num5 = 0.0 - num5;
		}
		if (num3 > Math.Abs(num4))
		{
			double num6 = Math.Acos(num4 / num3);
			angle_A = Kepler.NormalizeAngle(A.arg + num5 - num6);
			angle_B = Kepler.NormalizeAngle(A.arg + num5 + num6);
			return true;
		}
		angle_A = (angle_B = GetIntersectionAngle(A, B));
		return false;
	}

	public static double GetIntersectionAngle(Orbit A, Orbit B)
	{
		Double3[] array = new Double3[3]
		{
			A.arg_Matrix.Apply(new Double3(0.0, 0.0 - A.slr, A.slr)),
			A.arg_Matrix.Apply(new Double3(A.periapsis, 0.0, A.periapsis)),
			A.arg_Matrix.Apply(new Double3(0.0, A.slr, A.slr))
		};
		Double3[] array2 = new Double3[3]
		{
			B.arg_Matrix.Apply(new Double3(0.0, 0.0 - B.slr, B.slr)),
			B.arg_Matrix.Apply(new Double3(B.periapsis, 0.0, B.periapsis)),
			B.arg_Matrix.Apply(new Double3(0.0, B.slr, B.slr))
		};
		Double3 normal = GetNormal(array[0], array[1], array[2]);
		Double3 normal2 = GetNormal(array2[0], array2[1], array2[2]);
		Double3 @double = Double3.Cross(normal, normal2);
		@double /= Math.Max(Math.Abs(@double.x), Math.Abs(@double.y));
		Double3 double2 = array[1] - array[0];
		Double3 double3 = array[0];
		double num = (Double3.Dot(normal2, array2[0]) - Double3.Dot(normal2, double3)) / Double3.Dot(normal2, double2);
		Double3 double4 = (double3 + double2 * num).Rotate(0.0 - A.arg);
		@double = @double.Rotate(0.0 - A.arg);
		double4.x += A.sma - A.periapsis;
		double semiMinorAxis = Kepler.GetSemiMinorAxis(A.sma, A.ecc);
		double num2 = Math.Pow(@double.x, 2.0) / (A.sma * A.sma) + Math.Pow(@double.y, 2.0) / Math.Pow(semiMinorAxis, 2.0);
		double num3 = 2.0 * double4.x * @double.x / (A.sma * A.sma) + 2.0 * double4.y * @double.y / Math.Pow(semiMinorAxis, 2.0);
		return ((Double2)(double4 + @double * ((0.0 - num3) / 2.0 / num2)) - new Double2(A.sma - A.periapsis, 0.0)).AngleRadians + A.arg;
	}

	private static Double3 GetNormal(Double3 a, Double3 b, Double3 c)
	{
		return Double3.Cross(a - b, a - c);
	}
}
