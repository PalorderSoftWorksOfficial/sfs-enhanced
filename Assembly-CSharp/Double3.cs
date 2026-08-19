using System;
using UnityEngine;

[Serializable]
public struct Double3
{
	public double x;

	public double y;

	public double z;

	public static Double3 zero => default(Double3);

	public double magnitude2d => Math.Sqrt(x * x + y * y);

	public double magnitude => Math.Sqrt(x * x + y * y + z * z);

	public double sqrMagnitude => x * x + y * y + z * z;

	public double sqrMagnitude2d => x * x + y * y;

	public Double3 normalized2d
	{
		get
		{
			double num = magnitude2d;
			if (num > 9.99999974737875E-06)
			{
				return this / num;
			}
			return zero;
		}
	}

	public Double3 normalized
	{
		get
		{
			double num = magnitude;
			if (num > 9.99999974737875E-06)
			{
				return this / num;
			}
			return zero;
		}
	}

	public Double3(double x, double y, double z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Double3(double x, double y)
	{
		this.x = x;
		this.y = y;
		z = 0.0;
	}

	public static Double3 Cross(Double3 a, Double3 b)
	{
		return new Double3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	public static Double3 Cross(Double2 a, Double2 b)
	{
		return new Double3(0.0, 0.0, a.x * b.y - a.y * b.x);
	}

	public static Double3 GetPerpendicular(Double3 point_A, Double3 point_B, Double3 point_C)
	{
		Double3 a = point_A - point_B;
		Double3 b = point_A - point_C;
		return Cross(a, b);
	}

	public static double Dot(Double3 a, Double3 b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	public static Double3 CosSin(double a)
	{
		return new Double3(Math.Cos(a), Math.Sin(a));
	}

	public double AngleRadians()
	{
		return Math.Atan2(y, x);
	}

	public Double3 Rotate(double angleRadians)
	{
		double num = Math.Cos(angleRadians);
		double num2 = Math.Sin(angleRadians);
		return new Double3(x * num - y * num2, x * num2 + y * num);
	}

	public static Double3 operator +(Double3 a, Double3 b)
	{
		return new Double3(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static Double3 operator +(Double3 a, Vector3 b)
	{
		return new Double3(a.x + (double)b.x, a.y + (double)b.y, a.z + (double)b.z);
	}

	public static Double3 operator +(Double3 a, Vector2 b)
	{
		return new Double3(a.x + (double)b.x, a.y + (double)b.y, a.z);
	}

	public static Double3 operator -(Double3 a, Double3 b)
	{
		return new Double3(a.x - b.x, a.y - b.y, a.z - b.z);
	}

	public static Double3 operator -(Double3 a, Vector3 b)
	{
		return new Double3(a.x - (double)b.x, a.y - (double)b.y, a.z - (double)b.z);
	}

	public static Double3 operator -(Double3 a, Vector2 b)
	{
		return new Double3(a.x - (double)b.x, a.y - (double)b.y, a.z);
	}

	public static Double3 operator -(Double3 a)
	{
		return new Double3(0.0 - a.x, 0.0 - a.y, 0.0 - a.z);
	}

	public static Double3 operator *(Double3 a, double b)
	{
		return new Double3(a.x * b, a.y * b, a.z * b);
	}

	public static Double3 operator *(double d, Double3 a)
	{
		return new Double3(a.x * d, a.y * d, a.z * d);
	}

	public static Double3 operator /(Double3 a, double b)
	{
		return new Double3(a.x / b, a.y / b, a.z / b);
	}

	public static bool operator ==(Double3 a, Double3 b)
	{
		if (a.x == b.x && a.y == b.y)
		{
			return a.z == b.z;
		}
		return false;
	}

	public static bool operator !=(Double3 a, Double3 b)
	{
		if (a.x == b.x && a.y == b.y)
		{
			return a.z != b.z;
		}
		return true;
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ (y.GetHashCode() << 2);
	}

	public override bool Equals(object other)
	{
		if (!(other is Double3))
		{
			return false;
		}
		return Equals((Double3)other);
	}

	public bool Equals(Double3 other)
	{
		if (x.Equals(other.x))
		{
			return y.Equals(other.y);
		}
		return false;
	}

	public static implicit operator Vector2(Double3 a)
	{
		return new Vector2((float)a.x, (float)a.y);
	}

	public static implicit operator Vector3(Double3 a)
	{
		return new Vector3((float)a.x, (float)a.y, (float)a.z);
	}

	public static Double3 ToDouble3(Vector3 a)
	{
		return new Double3(a.x, a.y, a.z);
	}

	public static double GetClosestPointOnLine(Double3 p2, Double3 p3)
	{
		double num = p2.x;
		double num2 = p2.y;
		double num3 = num * num + num2 * num2;
		double num4 = (p3.x * num + p3.y * num2) / num3;
		if (num4 < 0.0)
		{
			return 0.0;
		}
		if (num4 > 1.0)
		{
			return 1.0;
		}
		return num4;
	}

	public override string ToString()
	{
		return "(" + x + ", " + y + ", " + z + ")";
	}

	public static explicit operator Double3(Double2 a)
	{
		return new Double3(a.x, a.y);
	}
}
