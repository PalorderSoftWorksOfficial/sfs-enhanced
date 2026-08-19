using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class MassModule : MonoBehaviour, I_InitializePartModule
{
	public PolygonData polygonModule;

	public float density = 1.25f;

	public Float_Reference mass;

	int I_InitializePartModule.Priority => -1;

	void I_InitializePartModule.Initialize()
	{
		polygonModule.onChange += new Action(RecalculateMass);
	}

	private void RecalculateMass()
	{
		float num = (GetArea() * density).Round(0.01f);
		if (float.IsNaN(num) || num == 0f)
		{
			num = 0.01f;
		}
		mass.Value = num;
	}

	private float GetArea()
	{
		Vector2[] vertices = polygonModule.polygon.vertices;
		float num = 0f;
		for (int i = 0; i < vertices.Length - 1; i++)
		{
			num += (vertices[i + 1].x - vertices[i].x) * (vertices[i + 1].y + vertices[i].y) / 2f;
		}
		num += (vertices[0].x - vertices[vertices.Length - 1].x) * (vertices[0].y + vertices[vertices.Length - 1].y) / 2f;
		return Mathf.Abs(num);
	}
}
