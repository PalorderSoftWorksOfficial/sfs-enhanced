using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class MagnetModule : MonoBehaviour
{
	[Serializable]
	public class Point
	{
		public Composed_Vector2 position;

		[NonSerialized]
		public bool occupied;
	}

	private const float Range = 1f;

	public Point[] points;

	public Vector2[] GetSnapPointsWorld()
	{
		return ((IEnumerable<Point>)points).Select((Func<Point, Vector2>)((Point p) => base.transform.TransformPoint(p.position.Value))).ToArray();
	}

	public static List<Vector2> GetAllSnapOffsets(MagnetModule[] A, MagnetModule[] B, float snapDistance)
	{
		List<Vector2> list = new List<Vector2>();
		Dictionary<Vector2Int, List<(Vector2, Point)>> dictionary = GetDictionary(B, 1f);
		for (int i = 0; i < A.Length; i++)
		{
			Vector2[] snapPointsWorld = A[i].GetSnapPointsWorld();
			foreach (Vector2 vector in snapPointsWorld)
			{
				Vector2Int[] dictionaryKeys = GetDictionaryKeys(vector, 1f);
				foreach (Vector2Int key in dictionaryKeys)
				{
					if (!dictionary.TryGetValue(key, out var value))
					{
						continue;
					}
					foreach (var item2 in value)
					{
						if (!item2.Item2.occupied)
						{
							Vector2 item = item2.Item1 - vector;
							if (item.sqrMagnitude <= snapDistance * snapDistance)
							{
								list.Add(item);
							}
						}
					}
				}
			}
		}
		return list;
	}

	public static void UpdateOccupied(Part[] parts)
	{
		MagnetModule[] array = Part_Utility.GetModules<MagnetModule>(parts).ToArray();
		Dictionary<Vector2Int, List<(Vector2, Point)>> dictionary = GetDictionary(array, 1f);
		MagnetModule[] array2 = array;
		foreach (MagnetModule module in array2)
		{
			Point[] array3 = module.points;
			foreach (Point point in array3)
			{
				point.occupied = GetOccupied();
				bool GetOccupied()
				{
					Vector2 vector = module.transform.TransformPoint(point.position.Value);
					Vector2Int[] dictionaryKeys = GetDictionaryKeys(vector, 1f);
					foreach (Vector2Int key in dictionaryKeys)
					{
						if (dictionary.TryGetValue(key, out var value))
						{
							foreach (var item in value)
							{
								if ((item.Item1 - vector).sqrMagnitude < 0.0025000002f && point != item.Item2)
								{
									return true;
								}
							}
						}
					}
					return false;
				}
			}
		}
	}

	private static Dictionary<Vector2Int, List<(Vector2, Point)>> GetDictionary(MagnetModule[] modules, float range)
	{
		Dictionary<Vector2Int, List<(Vector2, Point)>> dictionary = new Dictionary<Vector2Int, List<(Vector2, Point)>>();
		foreach (MagnetModule magnetModule in modules)
		{
			Point[] array = magnetModule.points;
			foreach (Point point in array)
			{
				Vector2 item = magnetModule.transform.TransformPoint(point.position.Value);
				Vector2Int key = new Vector2Int(Mathf.FloorToInt(item.x / range), Mathf.FloorToInt(item.y / range));
				(Vector2, Point) item2 = (item, point);
				if (dictionary.TryGetValue(key, out var value))
				{
					value.Add(item2);
					continue;
				}
				value = new List<(Vector2, Point)> { item2 };
				dictionary.Add(key, value);
			}
		}
		return dictionary;
	}

	public static Vector2Int[] GetDictionaryKeys(Vector2 position, float range)
	{
		int num = Mathf.RoundToInt(position.x / range);
		int num2 = Mathf.RoundToInt(position.y / range);
		int x = num - 1;
		int y = num2 - 1;
		return new Vector2Int[4]
		{
			new Vector2Int(x, y),
			new Vector2Int(num, y),
			new Vector2Int(x, num2),
			new Vector2Int(num, num2)
		};
	}
}
