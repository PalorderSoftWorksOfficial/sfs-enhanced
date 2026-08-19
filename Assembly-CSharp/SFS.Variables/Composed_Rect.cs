using System;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public class Composed_Rect : Composed<Rect>
{
	public Composed_Vector2 position;

	public Composed_Vector2 size;

	protected override Rect GetResult(bool initialize)
	{
		if (initialize)
		{
			position.OnChange += new Action(base.Recalculate);
			size.OnChange += new Action(base.Recalculate);
		}
		return new Rect(position.Value, size.Value);
	}

	protected override bool Equals(Rect a, Rect b)
	{
		return false;
	}
}
