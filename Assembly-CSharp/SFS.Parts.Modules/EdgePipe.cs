using System;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

public class EdgePipe : PipeData, I_InitializePartModule
{
	public SurfaceData surfaceData;

	public float width = 1f;

	public bool flatStart;

	public bool flatEnd;

	int I_InitializePartModule.Priority => 8;

	void I_InitializePartModule.Initialize()
	{
		surfaceData.onChange += new Action(Output);
	}

	public override void Output()
	{
		if (!Application.isPlaying)
		{
			surfaceData.Output();
		}
		if (surfaceData.surfaces == null)
		{
			surfaceData.Output();
		}
		Vector2[] points = surfaceData.surfaces[0].points;
		Vector2[] array = new Vector2[points.Length - 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (points[i + 1] - points[i]).Rotate_90().normalized;
		}
		Pipe pipe = new Pipe();
		pipe.AddPoint_SideAnchor(points[0], flatStart ? A(points[0], Vector2.right, array[0]) : (array[0] * width));
		for (int j = 1; j < points.Length - 1; j++)
		{
			pipe.AddPoint_SideAnchor(points[j], GetWidth(points[j], array[j - 1], array[j]));
		}
		pipe.AddPoint_SideAnchor(points.Last(), flatEnd ? A(points.Last(), Vector2.left, array.Last()) : (array.Last() * width));
		SetData(pipe);
		Vector2 A(Vector2 p, Vector2 line, Vector2 normal_A)
		{
			Vector2 vector = p + normal_A * width;
			Vector2 lineIntersection = Math_Utility.GetLineIntersection(p, p + line, vector, vector + normal_A.Rotate_90(), out var parallel);
			return (parallel ? vector : lineIntersection) - p;
		}
		Vector2 GetWidth(Vector2 p, Vector2 normal_A, Vector2 normal_B)
		{
			Vector2 vector = p + normal_A * width;
			Vector2 vector2 = p + normal_B * width;
			Vector2 lineIntersection = Math_Utility.GetLineIntersection(vector, vector + normal_A.Rotate_90(), vector2, vector2 + normal_B.Rotate_90(), out var parallel);
			return (parallel ? vector : lineIntersection) - p;
		}
	}
}
