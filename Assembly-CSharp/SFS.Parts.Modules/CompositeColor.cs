using UnityEngine;

namespace SFS.Parts.Modules;

public class CompositeColor : ColorModule
{
	public ColorModule[] colors;

	public override Color GetColor()
	{
		Color white = Color.white;
		ColorModule[] array = colors;
		foreach (ColorModule colorModule in array)
		{
			white *= colorModule.GetColor();
		}
		return white;
	}
}
