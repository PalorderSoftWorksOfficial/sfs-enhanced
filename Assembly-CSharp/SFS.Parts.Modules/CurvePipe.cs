using System;
using System.Collections.Generic;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class CurvePipe : PipeData, I_InitializePartModule
{
	public List<Composed_Vector2> points;

	public int[] pointCount;

	public Composed_Vector2 scale = new Composed_Vector2(Vector2.one);

	public bool edit = true;

	public bool view = true;

	public float gridSize = 0.1f;

	int I_InitializePartModule.Priority => 10;

	void I_InitializePartModule.Initialize()
	{
		foreach (Composed_Vector2 point in points)
		{
			point.OnChange += new Action(Output);
		}
		scale.OnChange += new Action(Output);
		Output();
	}

	public override void Output()
	{
		Pipe pipe = new Pipe();
		Vector2[] array = GetPoints();
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 vector = array[i];
			pipe.AddPoint(new Vector2(0f, vector.y), new Vector2(vector.x * 2f, 0f));
		}
		SetData(pipe);
	}

	public Vector2[] GetPoints()
	{
		List<Vector2> list = new List<Vector2>();
		Vector2 value = scale.Value;
		for (int i = 0; i < points.Count - 1; i += 2)
		{
			Vector2[] input = new Vector2[3]
			{
				points[i].Value,
				points[i + 1].Value,
				points[i + 2].Value
			};
			int num = pointCount[i / 2];
			for (int j = ((i != 0) ? 1 : 0); j < num; j++)
			{
				float t = (float)j / (float)(num - 1);
				list.Add(Reduce(input)[0] * value);
				Vector2[] Reduce(Vector2[] array)
				{
					while (array.Length > 1)
					{
						Vector2[] array2 = new Vector2[array.Length - 1];
						for (int k = 0; k < array.Length - 1; k++)
						{
							array2[k] = Vector2.Lerp(array[k], array[k + 1], t);
						}
						array = array2;
					}
					return array;
				}
			}
		}
		return list.ToArray();
	}
}
