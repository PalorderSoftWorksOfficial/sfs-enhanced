using UnityEngine;

namespace SFS.Parts.Modules;

public class FramingOverwrite : MonoBehaviour
{
	public Vector2 a;

	public Vector2 b;

	public Rect GetBounds_WorldSpace()
	{
		return new Rect(base.transform.TransformPoint(a), base.transform.TransformVector(b - a));
	}
}
