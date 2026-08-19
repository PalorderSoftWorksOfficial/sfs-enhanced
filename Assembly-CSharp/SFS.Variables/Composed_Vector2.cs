using System;
using System.Globalization;
using UnityEngine;

namespace SFS.Variables;

[Serializable]
public class Composed_Vector2 : Composed<Vector2>
{
	public Composed_Float x;

	public Composed_Float y;

	protected override Vector2 GetResult(bool initialize)
	{
		if (initialize)
		{
			x.OnChange += new Action(base.Recalculate);
			y.OnChange += new Action(base.Recalculate);
		}
		return new Vector2(x.Value, y.Value);
	}

	protected override bool Equals(Vector2 a, Vector2 b)
	{
		return a == b;
	}

	public Composed_Vector2(Vector2 a)
	{
		x = new Composed_Float(a.x.ToString(CultureInfo.InvariantCulture));
		y = new Composed_Float(a.y.ToString(CultureInfo.InvariantCulture));
	}

	public void Offset(Vector2 offset)
	{
		x.Offset(offset.x);
		y.Offset(offset.y);
	}
}
