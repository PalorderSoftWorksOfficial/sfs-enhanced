using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace Parts.Transform;

public class CraftFlipModule : MonoBehaviour, Rocket.INJ_Rocket
{
	public Rocket Rocket { private get; set; }

	public void Flip()
	{
		Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, useLaunchBounds: true, Rocket.partHolder.GetArray());
		Vector2 pivot = Rocket.partHolder.transform.InverseTransformPoint(bounds.center.Round(new Vector2(0.25f, 0.25f)));
		Part_Utility.ApplyOrientationChange(new Orientation(-1f, 1f, 0f), pivot, Rocket.partHolder.parts);
	}
}
