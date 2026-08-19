using UnityEngine;

public static class Transform_Utility
{
	public static Vector2 LocalToLocalPoint(Component a, Component b, Vector2 point)
	{
		return b.transform.InverseTransformPoint(a.transform.TransformPoint(point));
	}

	public static Vector2 TransformVectorUnscaled(this Transform a, Vector2 force)
	{
		return a.TransformVector(force / new Vector2(Mathf.Abs(a.lossyScale.x), Mathf.Abs(a.lossyScale.y)));
	}

	public static float RotationDirection(this Transform a)
	{
		return Mathf.Sign(a.lossyScale.x * a.lossyScale.y);
	}
}
