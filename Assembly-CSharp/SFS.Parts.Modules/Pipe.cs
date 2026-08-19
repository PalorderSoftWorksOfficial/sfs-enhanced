using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

public class Pipe
{
	public List<PipePoint> points;

	public Pipe()
	{
		points = new List<PipePoint>();
	}

	public void AddPoint_SideAnchor(Vector2 position, Vector2 width)
	{
		AddPoint(position - width / 2f, width);
	}

	public void AddPoint(Vector2 position, Vector2 width)
	{
		points.Add(new PipePoint(position, width, (points.Count > 0) ? (points.Last().height + (points.Last().position - position).magnitude) : 0f, 0f, 1f));
	}

	public Vector2 GetWidthAtHeight(float height)
	{
		for (int i = 0; i < points.Count - 1; i++)
		{
			if (points[i + 1].height > height)
			{
				return PipePoint.LerpByHeight(points[i], points[i + 1], height).width;
			}
		}
		return points.Last().width;
	}
}
