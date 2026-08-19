using UnityEngine;

public class Matrix2x2
{
	private Vector2 x;

	private Vector2 y;

	public static Matrix2x2 Angle(float angleRadians)
	{
		float num = Mathf.Cos(angleRadians);
		float num2 = Mathf.Sin(angleRadians);
		return new Matrix2x2
		{
			x = new Vector2(num, num2),
			y = new Vector2(0f - num2, num)
		};
	}

	public static Vector2 operator *(Matrix2x2 matrix, Vector2 b)
	{
		return new Vector2(matrix.x.x * b.x + matrix.y.x * b.y, matrix.x.y * b.x + matrix.y.y * b.y);
	}

	public static Vector2 operator *(Vector2 b, Matrix2x2 matrix)
	{
		return new Vector2(matrix.x.x * b.x + matrix.y.x * b.y, matrix.x.y * b.x + matrix.y.y * b.y);
	}

	public static Vector2 operator *(Matrix2x2 matrix, Vector3 b)
	{
		return new Vector2(matrix.x.x * b.x + matrix.y.x * b.y, matrix.x.y * b.x + matrix.y.y * b.y);
	}

	public static Vector2 operator *(Vector3 b, Matrix2x2 matrix)
	{
		return new Vector2(matrix.x.x * b.x + matrix.y.x * b.y, matrix.x.y * b.x + matrix.y.y * b.y);
	}

	public float GetX(Vector2 b)
	{
		return x.x * b.x + y.x * b.y;
	}
}
