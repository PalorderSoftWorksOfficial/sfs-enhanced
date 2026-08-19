using SFS.Input;
using UnityEngine;

namespace SFS.UI;

public static class UI_Raycaster
{
	public static void RaycastScreenClamped(Vector2 origin, Vector2 ray, RectTransform safeArea, float border, out Vector2 hitPos)
	{
		if (GetBounds_World(safeArea.GetRect(), 0f - border).IntersectRay(new Ray(origin + ray, -ray), out var distance) && distance > 0f)
		{
			ray = ray.normalized * (ray.magnitude - distance);
		}
		float bestDistance = ray.magnitude;
		foreach (Raycastable raycastable in Raycastable.raycastables)
		{
			Raycast(origin, ray, border, ref bestDistance, raycastable);
		}
		foreach (I_Touchable element in TouchElements.elements)
		{
			if (element is I_Raycastable { SkipRaycast: false } i_Raycastable)
			{
				Raycast(origin, ray, border, ref bestDistance, i_Raycastable);
			}
		}
		hitPos = origin + ray.normalized * bestDistance;
	}

	private static void Raycast(Vector2 origin, Vector2 ray, float border, ref float bestDistance, I_Raycastable raycastable)
	{
		if (GetBounds_World(raycastable.GetRect(), border).IntersectRay(new Ray(origin, ray), out var distance) && distance < bestDistance)
		{
			bestDistance = distance;
		}
	}

	private static Bounds GetBounds_World(RectTransform rect, float border)
	{
		Vector3[] array = new Vector3[4];
		rect.GetWorldCorners(array);
		Vector2 vector = (array[0] + array[2]) * 0.5f;
		return new Bounds(size: new Vector2(Mathf.Abs(array[3].x - array[0].x), Mathf.Abs(array[2].y - array[0].y)) + Vector2.one * (border * 2f), center: vector);
	}
}
