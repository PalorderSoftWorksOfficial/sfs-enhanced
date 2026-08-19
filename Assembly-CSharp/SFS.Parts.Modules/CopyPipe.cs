using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public class CopyPipe : PipeData, I_InitializePartModule
{
	public PipeData input;

	int I_InitializePartModule.Priority => 8;

	void I_InitializePartModule.Initialize()
	{
		input.onChange += new Action(Output);
	}

	public override void Output()
	{
		if (!Application.isPlaying)
		{
			input.Output();
		}
		Pipe pipe = input.pipe;
		Pipe pipe2 = new Pipe
		{
			points = new List<PipePoint>(pipe.points.Count)
		};
		for (int i = 0; i < pipe.points.Count; i++)
		{
			PipePoint pipePoint = pipe.points[i];
			pipe2.points.Add(new PipePoint(pipePoint.position, pipePoint.width, pipePoint.height, pipePoint.cutLeft, pipePoint.cutRight));
		}
		SetData(pipe2);
	}
}
