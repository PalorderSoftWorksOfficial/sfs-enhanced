using UnityEngine;

public class Polygon
{
	public readonly ConvexPolygon[] convexPolygons;

	public readonly Vector2[] vertices;

	public Polygon(params Vector2[] vertices)
	{
		this.vertices = vertices;
		convexPolygons = PolygonPartioner.Partion(vertices).ToArray();
	}

	public static bool Intersect(ConvexPolygon[] A, ConvexPolygon[] B, float overlapThreshold)
	{
		foreach (ConvexPolygon convexPolygon in A)
		{
			foreach (ConvexPolygon convexPolygon2 in B)
			{
				if (convexPolygon.points.Length == 0 || convexPolygon2.points.Length == 0)
				{
					Debug.Log("Length 0");
				}
				else if (ConvexPolygon.Intersect(convexPolygon, convexPolygon2, overlapThreshold))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool Intersect(Vector2 position, Vector2 ray, ConvexPolygon[] B, float overlapThreshold)
	{
		for (int i = 0; i < B.Length; i++)
		{
			if (B[i].Intersects(position, position + ray, overlapThreshold))
			{
				return true;
			}
		}
		return false;
	}

	public static float GetDistanceToPolygons(Vector2 point, params ConvexPolygon[] polygons)
	{
		float num = float.PositiveInfinity;
		for (int i = 0; i < polygons.Length; i++)
		{
			float distanceToPolygon = polygons[i].GetDistanceToPolygon(point);
			if (distanceToPolygon < num)
			{
				num = distanceToPolygon;
			}
		}
		return num;
	}

	public Vector2[] GetVerticesWorld(Transform transform)
	{
		Vector2[] array = new Vector2[vertices.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = transform.TransformPoint(vertices[i]);
		}
		return array;
	}

	public ConvexPolygon[] GetConvexPolygonsWorld(Transform transform)
	{
		ConvexPolygon[] array = new ConvexPolygon[convexPolygons.Length];
		for (int i = 0; i < array.Length; i++)
		{
			ConvexPolygon convexPolygon = convexPolygons[i];
			Vector2[] array2 = new Vector2[convexPolygon.points.Length];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = transform.TransformPoint(convexPolygon.points[j]);
			}
			array[i] = new ConvexPolygon(array2);
		}
		return array;
	}
}
