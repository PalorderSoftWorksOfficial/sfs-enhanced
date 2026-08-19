using UnityEngine;

namespace SFS.Parts.Modules;

public class BoxPolygon : PolygonData, I_InitializePartModule
{
	public Vector2 point_A = Vector2.zero;

	public Vector2 point_B = Vector2.one;

	int I_InitializePartModule.Priority => 10;

	void I_InitializePartModule.Initialize()
	{
		Output();
	}

	public override void Output()
	{
		Polygon vertices = GetVertices();
		SetData(vertices, vertices);
	}

	public Polygon GetVertices()
	{
		return new Polygon(point_A, new Vector2(point_A.x, point_B.y), point_B, new Vector2(point_B.x, point_A.y));
	}
}
