using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class SimplePipe : PipeData, I_InitializePartModule
{
	public Composed_Float width_a;

	public Composed_Float width_b;

	public Composed_Float height_a;

	public Composed_Float height_b;

	int I_InitializePartModule.Priority => 10;

	void I_InitializePartModule.Initialize()
	{
		width_a.OnChange += new Action(Output);
		width_b.OnChange += new Action(Output);
		height_a.OnChange += new Action(Output);
		height_b.OnChange += new Action(Output);
	}

	public override void Output()
	{
		Pipe pipe = new Pipe();
		pipe.AddPoint(new Vector2(0f, height_a.Value), Vector2.right * width_a.Value);
		pipe.AddPoint(new Vector2(0f, height_b.Value), Vector2.right * width_b.Value);
		SetData(pipe);
	}
}
