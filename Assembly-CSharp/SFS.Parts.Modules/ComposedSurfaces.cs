using System;
using System.Linq;
using SFS.Variables;

namespace SFS.Parts.Modules;

[Serializable]
public class ComposedSurfaces
{
	public Composed_Vector2[] points = new Composed_Vector2[2];

	public bool loop;

	private void Reverse()
	{
		points = points.Reverse().ToArray();
	}
}
