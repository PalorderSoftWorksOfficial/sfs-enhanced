using System;

namespace SFS.World.Maps;

[Serializable]
public class LandmarkData
{
	public string name;

	public float angle;

	public float startAngle;

	public float endAngle;

	public float Center => (float)Math_Utility.NormalizePositiveAngleDegrees(startAngle + AngularWidth / 2f);

	public float AngularWidth => (float)Math_Utility.NormalizeAngleDegrees(endAngle - startAngle);
}
