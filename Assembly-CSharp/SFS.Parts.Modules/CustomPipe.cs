using System;
using System.Collections.Generic;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class CustomPipe : PipeData, I_InitializePartModule
{
	[Space(2f)]
	public Composed_Pipe composedShape = new Composed_Pipe
	{
		points = new List<Composed_PipePoint>
		{
			new Composed_PipePoint(Vector2.zero, Vector2.right),
			new Composed_PipePoint(Vector2.up, Vector2.right)
		}
	};

	public bool edit = true;

	public bool view = true;

	public float gridSize = 0.1f;

	int I_InitializePartModule.Priority => 10;

	void I_InitializePartModule.Initialize()
	{
		composedShape.OnChange += new Action(Output);
	}

	public override void Output()
	{
		SetData(composedShape.Value);
	}
}
