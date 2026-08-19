using System;
using System.Collections.Generic;
using SFS.Parts.Modules;

namespace SFS.Variables;

[Serializable]
public class Composed_Pipe : Composed<Pipe>
{
	public List<Composed_PipePoint> points;

	protected override Pipe GetResult(bool initialize)
	{
		if (initialize)
		{
			foreach (Composed_PipePoint point in points)
			{
				point.OnChange += new Action(base.Recalculate);
			}
		}
		Pipe pipe = new Pipe();
		foreach (Composed_PipePoint point2 in points)
		{
			pipe.AddPoint(point2.Value.position, point2.Value.width);
		}
		return pipe;
	}

	protected override bool Equals(Pipe a, Pipe b)
	{
		return false;
	}
}
