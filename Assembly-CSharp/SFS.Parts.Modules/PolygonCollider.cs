using System;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class PolygonCollider : ColliderModule, I_InitializePartModule
{
	public PolygonData polygon;

	[SerializeField]
	[HideInInspector]
	private PolygonCollider2D collider_Polygon;

	[SerializeField]
	[HideInInspector]
	private BoxCollider2D collider_Box;

	int I_InitializePartModule.Priority => -1;

	void I_InitializePartModule.Initialize()
	{
		if (GameManager.main != null)
		{
			polygon.onChange += new Action(BuildCollider);
		}
	}

	private void BuildCollider()
	{
		Vector2[] vertices = polygon.polygonFast.vertices;
		if (vertices.Length == 4 && vertices[0].x == vertices[1].x && vertices[1].y == vertices[2].y && vertices[2].x == vertices[3].x && vertices[3].y == vertices[0].y)
		{
			if (collider_Polygon != null)
			{
				UnityEngine.Object.Destroy(collider_Polygon);
			}
			if (collider_Box == null)
			{
				collider_Box = base.gameObject.AddComponent<BoxCollider2D>();
			}
			collider_Box.size = new Vector2(Mathf.Abs(vertices[2].x - vertices[0].x), Mathf.Abs(vertices[2].y - vertices[0].y));
			collider_Box.offset = (vertices[0] + vertices[2]) * 0.5f;
		}
		else
		{
			if (collider_Box != null)
			{
				UnityEngine.Object.Destroy(collider_Polygon);
			}
			if (collider_Polygon == null)
			{
				collider_Polygon = base.gameObject.AddComponent<PolygonCollider2D>();
			}
			collider_Polygon.points = vertices;
		}
	}
}
