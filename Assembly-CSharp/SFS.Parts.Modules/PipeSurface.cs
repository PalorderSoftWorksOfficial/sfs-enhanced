using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.Parts.Modules;

public class PipeSurface : SurfaceData, I_InitializePartModule
{
	[Space]
	public PipeData pipeData;

	public bool top = true;

	public bool left = true;

	public bool bottom = true;

	public bool right = true;

	int I_InitializePartModule.Priority => 9;

	void I_InitializePartModule.Initialize()
	{
		pipeData.onChange += new Action(Output);
	}

	public override void Output()
	{
		if (!Application.isPlaying)
		{
			pipeData.Output();
		}
		if (pipeData.pipe == null)
		{
			pipeData.Output();
		}
		List<Vector2> list = pipeData.polygon.vertices.ToList();
		List<Vector2>[] array = new List<Vector2>[4]
		{
			left ? list.GetRange(0, list.Count / 2) : null,
			top ? list.GetRange(list.Count / 2 - 1, 2) : null,
			right ? list.GetRange(list.Count / 2, list.Count / 2) : null,
			bottom ? new List<Vector2>
			{
				list.Last(),
				list[0]
			} : null
		};
		for (int i = 0; i < 4; i++)
		{
			List<Vector2> list2 = array[i];
			List<Vector2> list3 = array[(i + 1) % 4];
			if (list2 != null && list3 != null && list2 != list3)
			{
				list2.AddRange(list3.GetRange(1, list3.Count - 1));
				array[(i + 1) % 4] = list2;
			}
		}
		List<Surfaces> list4 = new List<Surfaces>();
		List<Vector2>[] array2 = array;
		foreach (List<Vector2> list5 in array2)
		{
			if (list5 != null && list5.Count > 0)
			{
				list4.Add(new Surfaces(list5.ToArray(), loop: false, pipeData.transform));
				list5.Clear();
			}
		}
		SetData(list4, list4);
	}
}
