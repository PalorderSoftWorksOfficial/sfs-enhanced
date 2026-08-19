using UnityEngine;

namespace SFS.UI;

public abstract class ElementBuilder
{
	protected abstract void CreateElement(GameObject holder);

	public static implicit operator MenuElement(ElementBuilder builder)
	{
		if (builder != null)
		{
			return builder.CreateElement;
		}
		return null;
	}
}
