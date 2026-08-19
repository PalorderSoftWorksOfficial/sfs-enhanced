using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public class CustomPolygon : PolygonData, I_InitializePartModule
{
	public List<Vector2> polygonVertices = new List<Vector2>
	{
		Vector2.zero,
		Vector2.up,
		Vector2.right
	};

	int I_InitializePartModule.Priority => 10;

	void I_InitializePartModule.Initialize()
	{
		Output();
	}

	public override void Output()
	{
		SetData(new Polygon(polygonVertices.ToArray()));
	}
}
