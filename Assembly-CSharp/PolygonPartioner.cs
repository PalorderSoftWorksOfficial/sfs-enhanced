using System.Collections.Generic;
using UnityEngine;

public static class PolygonPartioner
{
	public static List<ConvexPolygon> Partion(Vector2[] points)
	{
		List<VertexChain> list = ConvexPartition(new VertexChain(points));
		List<ConvexPolygon> list2 = new List<ConvexPolygon>();
		for (int i = 0; i < list.Count; i++)
		{
			VertexChain vertexChain = list[i];
			Vector2[] array = new Vector2[vertexChain.Count];
			for (int j = 0; j < vertexChain.Count; j++)
			{
				array[j] = vertexChain[j];
			}
			list2.Add(new ConvexPolygon(array));
		}
		return list2;
	}

	private static float Area(ref Vector2 a, ref Vector2 b, ref Vector2 c)
	{
		return a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
	}

	private static Vector2 At(int position, VertexChain vertices)
	{
		int count = vertices.Count;
		return vertices[(position < 0) ? (count - -position % count) : (position % count)];
	}

	private static bool CanSee(int i, int j, VertexChain vertices)
	{
		Vector2 prev = At(i - 1, vertices);
		Vector2 vector = At(i, vertices);
		Vector2 next = At(i + 1, vertices);
		if (Reflex(prev, vector, next))
		{
			if (LeftOn(At(i, vertices), At(i - 1, vertices), At(j, vertices)) && RightOn(At(i, vertices), At(i + 1, vertices), At(j, vertices)))
			{
				return false;
			}
		}
		else if (RightOn(At(i, vertices), At(i + 1, vertices), At(j, vertices)) || LeftOn(At(i, vertices), At(i - 1, vertices), At(j, vertices)))
		{
			return false;
		}
		Vector2 prev2 = At(j - 1, vertices);
		Vector2 vector2 = At(j, vertices);
		Vector2 next2 = At(j + 1, vertices);
		if (Reflex(prev2, vector2, next2))
		{
			if (LeftOn(At(j, vertices), At(j - 1, vertices), At(i, vertices)) && RightOn(At(j, vertices), At(j + 1, vertices), At(i, vertices)))
			{
				return false;
			}
		}
		else if (RightOn(At(j, vertices), At(j + 1, vertices), At(i, vertices)) || LeftOn(At(j, vertices), At(j - 1, vertices), At(i, vertices)))
		{
			return false;
		}
		for (int k = 0; k < vertices.Count; k++)
		{
			Vector2 vector3 = default(Vector2);
			Vector2 point = default(Vector2);
			Vector2 point2 = At(i, vertices);
			Vector2 point3 = At(j, vertices);
			Vector2 point4 = At(k, vertices);
			Vector2 point5 = At(k + 1, vertices);
			if (!point2.Equals(point4) && !point2.Equals(point5) && !point3.Equals(point4) && !point3.Equals(point5))
			{
				LineIntersect(ref point2, ref point3, ref point4, ref point5, firstIsSegment: true, secondIsSegment: true, out point);
				if (point != vector3 && (!point.Equals(At(k, vertices)) || !point.Equals(At(k + 1, vertices))))
				{
					return false;
				}
			}
		}
		return true;
	}

	private static VertexChain CollinearSimplify(VertexChain vertices, float collinearityTolerance = 0f)
	{
		if (vertices.Count <= 3)
		{
			return vertices;
		}
		VertexChain vertexChain = new VertexChain(vertices.Count);
		for (int i = 0; i < vertices.Count; i++)
		{
			Vector2 a = vertices.PreviousVertex(i);
			Vector2 b = vertices[i];
			Vector2 c = vertices.NextVertex(i);
			if (!IsCollinear(ref a, ref b, ref c, collinearityTolerance))
			{
				vertexChain.Add(b);
			}
		}
		return vertexChain;
	}

	private static List<VertexChain> ConvexPartition(VertexChain vertices)
	{
		vertices.ForceCounterClockWise();
		List<VertexChain> list = new List<VertexChain>();
		if (vertices.Count < 3)
		{
			return list;
		}
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		Vector2 vector = default(Vector2);
		Vector2 vector2 = default(Vector2);
		int num4 = 0;
		int i = 0;
		for (int j = 0; j < vertices.Count; j++)
		{
			Vector2 prev = At(j - 1, vertices);
			Vector2 vector3 = At(j, vertices);
			Vector2 next = At(j + 1, vertices);
			if (!Reflex(prev, vector3, next))
			{
				continue;
			}
			num2 = (num3 = double.MaxValue);
			for (int k = 0; k < vertices.Count; k++)
			{
				if (j == k || j == Index(k - 1, vertices.Count) || j == Index(k + 1, vertices.Count))
				{
					continue;
				}
				Vector2 a = At(j - 1, vertices);
				Vector2 b = At(j, vertices);
				Vector2 c = At(k, vertices);
				Vector2 c2 = At(k - 1, vertices);
				bool flag = Left(a, b, c);
				bool flag2 = Right(a, b, c2);
				bool num5 = IsCollinear(ref a, ref b, ref c);
				bool flag3 = IsCollinear(ref a, ref b, ref c2);
				if (num5 | flag3)
				{
					num = SquareDist(b, c);
					if (num < num2)
					{
						num2 = num;
						vector = c;
						num4 = k - 1;
					}
					num = SquareDist(b, c2);
					if (num < num2)
					{
						num2 = num;
						vector = c2;
						num4 = k;
					}
				}
				else if (flag & flag2)
				{
					Vector2 vector4 = LineIntersect(At(j - 1, vertices), At(j, vertices), At(k, vertices), At(k - 1, vertices));
					if (Right(At(j + 1, vertices), At(j, vertices), vector4))
					{
						num = SquareDist(At(j, vertices), vector4);
						if (num < num2)
						{
							num2 = num;
							vector = vector4;
							num4 = k;
						}
					}
				}
				Vector2 a2 = At(j + 1, vertices);
				Vector2 c3 = At(k + 1, vertices);
				bool flag4 = Left(a2, b, c3);
				bool flag5 = Right(a2, b, c);
				bool num6 = IsCollinear(ref a2, ref b, ref c3);
				bool flag6 = IsCollinear(ref a2, ref b, ref c);
				if (num6 | flag6)
				{
					num = SquareDist(b, c3);
					if (num < num3)
					{
						num3 = num;
						vector2 = c3;
						i = k + 1;
					}
					num = SquareDist(At(j, vertices), At(k, vertices));
					if (num < num3)
					{
						num3 = num;
						vector2 = c;
						i = k;
					}
				}
				else
				{
					if (!(flag4 & flag5))
					{
						continue;
					}
					Vector2 vector4 = LineIntersect(At(j + 1, vertices), At(j, vertices), At(k, vertices), At(k + 1, vertices));
					if (Left(At(j - 1, vertices), At(j, vertices), vector4))
					{
						num = SquareDist(At(j, vertices), vector4);
						if (num < num3)
						{
							num3 = num;
							i = k;
							vector2 = vector4;
						}
					}
				}
			}
			VertexChain vertexChain;
			VertexChain vertexChain2;
			if (num4 == (i + 1) % vertices.Count)
			{
				Vector2 item = (vector + vector2) / 2f;
				vertexChain = Copy(j, i, vertices);
				vertexChain.Add(item);
				vertexChain2 = Copy(num4, j, vertices);
				vertexChain2.Add(item);
			}
			else
			{
				double num7 = 0.0;
				double num8 = num4;
				for (; i < num4; i += vertices.Count)
				{
				}
				for (int l = num4; l <= i; l++)
				{
					if (CanSee(j, l, vertices))
					{
						double num9 = 1f / (SquareDist(At(j, vertices), At(l, vertices)) + 1f);
						Vector2 prev2 = At(l - 1, vertices);
						Vector2 vector5 = At(l, vertices);
						Vector2 next2 = At(l + 1, vertices);
						num9 = ((!Reflex(prev2, vector5, next2)) ? (num9 + 1.0) : ((!RightOn(At(l - 1, vertices), At(l, vertices), At(j, vertices)) || !LeftOn(At(l + 1, vertices), At(l, vertices), At(j, vertices))) ? (num9 + 2.0) : (num9 + 3.0)));
						if (num9 > num7)
						{
							num8 = l;
							num7 = num9;
						}
					}
				}
				vertexChain = Copy(j, (int)num8, vertices);
				vertexChain2 = Copy((int)num8, j, vertices);
			}
			if (vertexChain.Count < vertexChain2.Count)
			{
				list.AddRange(ConvexPartition(vertexChain));
				list.AddRange(ConvexPartition(vertexChain2));
			}
			else
			{
				list.AddRange(ConvexPartition(vertexChain2));
				list.AddRange(ConvexPartition(vertexChain));
			}
			return list;
		}
		list.Add(vertices);
		for (int m = 0; m < list.Count; m++)
		{
			list[m] = CollinearSimplify(list[m], 1E-05f);
		}
		return list;
	}

	private static VertexChain Copy(int i, int j, VertexChain vertices)
	{
		VertexChain vertexChain = new VertexChain();
		while (j < i)
		{
			j += vertices.Count;
		}
		while (i <= j)
		{
			vertexChain.Add(At(i, vertices));
			i++;
		}
		return vertexChain;
	}

	private static bool FloatEquals(float value1, float value2)
	{
		return Mathf.Abs(value1 - value2) <= Mathf.Epsilon;
	}

	private static bool FloatInRange(float value, float min, float max)
	{
		if (value >= min)
		{
			return value <= max;
		}
		return false;
	}

	private static int Index(int i, int size)
	{
		if (i >= 0)
		{
			return i % size;
		}
		return size - -i % size;
	}

	private static bool IsCollinear(ref Vector2 a, ref Vector2 b, ref Vector2 c, float tolerance = 0f)
	{
		return FloatInRange(Area(ref a, ref b, ref c), 0f - tolerance, tolerance);
	}

	private static bool Left(Vector2 a, Vector2 b, Vector2 c)
	{
		return Area(ref a, ref b, ref c) > 0f;
	}

	private static bool LeftOn(Vector2 a, Vector2 b, Vector2 c)
	{
		return Area(ref a, ref b, ref c) >= 0f;
	}

	private static bool LineIntersect(ref Vector2 point1, ref Vector2 point2, ref Vector2 point3, ref Vector2 point4, bool firstIsSegment, bool secondIsSegment, out Vector2 point)
	{
		point = default(Vector2);
		float num = point4.y - point3.y;
		float num2 = point2.x - point1.x;
		float num3 = point4.x - point3.x;
		float num4 = point2.y - point1.y;
		float num5 = num * num2 - num3 * num4;
		if (!(num5 >= 0f - Mathf.Epsilon) || !(num5 <= Mathf.Epsilon))
		{
			float num6 = point1.y - point3.y;
			float num7 = point1.x - point3.x;
			float num8 = 1f / num5;
			float num9 = num3 * num6 - num * num7;
			num9 *= num8;
			if (!firstIsSegment || (num9 >= 0f && num9 <= 1f))
			{
				float num10 = num2 * num6 - num4 * num7;
				num10 *= num8;
				if ((!secondIsSegment || (num10 >= 0f && num10 <= 1f)) && (num9 != 0f || num10 != 0f))
				{
					point.x = point1.x + num9 * num2;
					point.y = point1.y + num9 * num4;
					return true;
				}
			}
		}
		return false;
	}

	private static Vector2 LineIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
	{
		Vector2 zero = Vector2.zero;
		float num = p2.y - p1.y;
		float num2 = p1.x - p2.x;
		float num3 = num * p1.x + num2 * p1.y;
		float num4 = q2.y - q1.y;
		float num5 = q1.x - q2.x;
		float num6 = num4 * q1.x + num5 * q1.y;
		float num7 = num * num5 - num4 * num2;
		if (!FloatEquals(num7, 0f))
		{
			zero.x = (num5 * num3 - num2 * num6) / num7;
			zero.y = (num * num6 - num4 * num3) / num7;
		}
		return zero;
	}

	private static bool Reflex(Vector2 prev, Vector2 on, Vector2 next)
	{
		if (IsCollinear(ref prev, ref on, ref next))
		{
			return false;
		}
		return Right(prev, on, next);
	}

	private static bool Right(Vector2 a, Vector2 b, Vector2 c)
	{
		return Area(ref a, ref b, ref c) < 0f;
	}

	private static bool RightOn(Vector2 a, Vector2 b, Vector2 c)
	{
		return Area(ref a, ref b, ref c) <= 0f;
	}

	private static float SquareDist(Vector2 a, Vector2 b)
	{
		float num = b.x - a.x;
		float num2 = b.y - a.y;
		return num * num + num2 * num2;
	}
}
