using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.UI;

[Serializable]
public class ScreenOrientation_Local : Obs<ScreenOrientation>
{
	protected override bool IsEqual(ScreenOrientation a, ScreenOrientation b)
	{
		return a == b;
	}
}
