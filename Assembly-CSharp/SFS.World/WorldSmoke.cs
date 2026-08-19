using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.World;

public class WorldSmoke : MonoBehaviour
{
	[Serializable]
	public class Point
	{
		public Double2 position;

		public Double2 velocity;
	}

	public List<Point> chain;

	public MeshFilter meshFilter;

	public MeshRenderer meshRenderer;

	public Vector3[] _vertices;

	private void Start()
	{
		meshFilter.mesh = new Mesh();
		meshRenderer.sortingLayerName = "Default";
		meshRenderer.sortingOrder = 10;
	}

	private void OnValidate()
	{
		Double2 zero = Double2.zero;
		List<Vector3> list = new List<Vector3>();
		int count = chain.Count;
		Vector2[] array = new Vector2[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = (chain[i].position - zero).ToVector2;
		}
		Vector2[] array2 = new Vector2[count - 1];
		for (int j = 0; j < count - 1; j++)
		{
			array2[j] = (array[j] - array[j + 1]).normalized.Rotate_90();
		}
		for (int k = 0; k < count - 1; k++)
		{
			Vector2 vector = array[k];
			Vector2 vector2;
			if (k > 0)
			{
				float num = ((Vector2.Dot(array2[k - 1], array2[k]) < 0.8f) ? 1f : 0.5f);
				Debug.Log(num);
				vector2 = Vector2.Lerp(array2[k - 1], array2[k], num);
			}
			else
			{
				vector2 = array2[0];
			}
			list.Add(vector + vector2);
			list.Add(vector - vector2);
			vector = array[k + 1];
			Vector2 vector3;
			if (k < count - 2)
			{
				float t = ((Vector2.Dot(array2[k], array2[k + 1]) < 0.8f) ? 0f : 0.5f);
				vector3 = Vector2.Lerp(array2[k], array2[k + 1], t);
			}
			else
			{
				vector3 = array2.Last();
			}
			list.Add(vector - vector3);
			list.Add(vector + vector3);
		}
		int[] array3 = new int[list.Count];
		for (int l = 0; l < list.Count; l++)
		{
			array3[l] = l;
		}
		Mesh mesh = meshFilter.mesh;
		mesh.SetVertices(list);
		mesh.SetIndices(array3, MeshTopology.Quads, 0);
		mesh.RecalculateBounds();
		_vertices = list.ToArray();
	}
}
