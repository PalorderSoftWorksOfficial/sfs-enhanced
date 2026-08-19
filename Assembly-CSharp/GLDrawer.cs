using System;
using System.Collections.Generic;
using UnityEngine;

public class GLDrawer : MonoBehaviour
{
	public static GLDrawer main;

	public Material material;

	private Dictionary<float, Material> sortedMaterials = new Dictionary<float, Material>();

	public List<I_GLDrawer> drawers = new List<I_GLDrawer>();

	private void Awake()
	{
		main = this;
	}

	public static void Register(I_GLDrawer drawer)
	{
		main.drawers.Add(drawer);
	}

	public static void Unregister(I_GLDrawer drawer)
	{
		main.drawers.Remove(drawer);
	}

	private void OnPostRender()
	{
		foreach (I_GLDrawer drawer in drawers)
		{
			drawer.Draw();
		}
	}

	public static void DrawLine(Vector3 start, Vector3 end, Color color, float width, float sortingOrder = 1f)
	{
		GL.Begin(7);
		GetMaterial(sortingOrder).SetPass(0);
		GL.Color(color);
		Vector3 vector = Vector2.Perpendicular((start - end).normalized) * width * 0.5f;
		GL.Vertex(start - vector);
		GL.Vertex(end - vector);
		GL.Vertex(end + vector);
		GL.Vertex(start + vector);
		GL.End();
	}

	public static void DrawCircle(Vector2 position, float radius, int resolution, Color color, float sortingOrder = 1f)
	{
		DrawCircles(new List<Vector2> { position }, radius, resolution, color, sortingOrder);
	}

	public static void DrawCircles(List<Vector2> positions, float radius, int resolution, Color color, float sortingOrder = 1f)
	{
		GL.Begin(7);
		GetMaterial(sortingOrder)?.SetPass(0);
		GL.Color(color);
		Vector2[] array = new Vector2[resolution];
		float num = (float)Math.PI * 2f / (float)resolution;
		for (int i = 0; i < resolution; i++)
		{
			array[i] = new Vector2(radius * Mathf.Cos(num * (float)i), radius * Mathf.Sin(num * (float)i));
		}
		foreach (Vector2 position in positions)
		{
			for (int j = 0; j < array.Length; j++)
			{
				GL.Vertex(position);
				GL.Vertex(position + array[(j + 1) % array.Length]);
				GL.Vertex(position + array[j]);
				GL.Vertex(position);
			}
		}
		GL.End();
	}

	private static Material GetMaterial(float sortingOrder)
	{
		if (!main)
		{
			return null;
		}
		if (!main.sortedMaterials.ContainsKey(sortingOrder))
		{
			Material material = new Material(main.material);
			material.SetFloat("_Depth", sortingOrder);
			main.sortedMaterials.Add(sortingOrder, material);
			return material;
		}
		return main.sortedMaterials[sortingOrder];
	}
}
