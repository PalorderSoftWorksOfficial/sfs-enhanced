using System;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public class Composed_PipePoint : Composed<PipePoint>
{
	public Composed_Vector2 position;

	public Composed_Vector2 width;

	protected override PipePoint GetResult(bool initialize)
	{
		if (initialize)
		{
			position.OnChange += new Action(base.Recalculate);
			width.OnChange += new Action(base.Recalculate);
		}
		return new PipePoint(position.Value, width.Value, float.NaN, float.NaN, float.NaN);
	}

	protected override bool Equals(PipePoint a, PipePoint b)
	{
		return false;
	}

	public Composed_PipePoint(Vector2 position, Vector2 width)
	{
		this.position = new Composed_Vector2(position);
		this.width = new Composed_Vector2(width);
	}
}
