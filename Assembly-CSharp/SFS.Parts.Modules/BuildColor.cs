using SFS.Builds;
using UnityEngine;

namespace SFS.Parts.Modules;

public class BuildColor : ColorModule
{
	public Color buildColor = Color.white;

	public override Color GetColor()
	{
		if (!(BuildManager.main != null))
		{
			return Color.white;
		}
		return buildColor;
	}
}
