public class Matrix2x2_Double
{
	private Double2 x;

	private Double2 y;

	public static Matrix2x2_Double Angle(double angleRadians)
	{
		return new Matrix2x2_Double
		{
			x = new Double2(1.0, 0.0).Rotate(angleRadians),
			y = new Double2(0.0, 1.0).Rotate(angleRadians)
		};
	}

	public Double2 Apply(Double2 a)
	{
		return a.x * x + a.y * y;
	}

	public Double3 Apply(Double3 a)
	{
		Double2 @double = a.x * x + a.y * y;
		return new Double3(@double.x, @double.y, a.z);
	}
}
