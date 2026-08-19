using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.UI;

public static class LayoutUtility
{
	public static Vector2 GetApplySize(Vector2 current, Vector2 newSize, bool apply_X, bool apply_Y)
	{
		return new Vector2(apply_X ? newSize.x : current.x, apply_Y ? newSize.y : current.y);
	}

	public static Vector2 GetFilteredSize(Vector2 current, IEnumerable<Vector2> elementSizes, SizeMode sizeMode)
	{
		List<Vector2> list = new List<Vector2>(elementSizes);
		if (list.Count == 0)
		{
			return current;
		}
		return sizeMode switch
		{
			SizeMode.AverageChildSize => new Vector2(list.Average((Vector2 e) => e.x), list.Average((Vector2 e) => e.y)), 
			SizeMode.MaxChildSize => new Vector2(list.Max((Vector2 e) => e.x), list.Max((Vector2 e) => e.y)), 
			_ => current, 
		};
	}
}
