using UnityEngine;

namespace SFS.UI;

public static class Rect_Utility
{
	public static RectTransform Rect(this GameObject A)
	{
		return A.transform.Rect();
	}

	public static RectTransform Rect(this Transform A)
	{
		if (A is RectTransform result)
		{
			return result;
		}
		return A.gameObject.AddComponent<RectTransform>();
	}

	public static void SetPosition(this RectTransform A, Vector2 position)
	{
		RectTransform component = A.transform.parent.GetComponent<RectTransform>();
		Vector2 vector = A.pivot * A.rect.size;
		Vector2 vector2 = component.pivot * component.rect.size;
		A.localPosition = vector - vector2 + position;
	}

	public static Vector2 GetLocalPosition(this RectTransform A)
	{
		RectTransform rectTransform = A.transform.parent.Rect();
		Vector2 vector = A.pivot * A.rect.size;
		Vector2 vector2 = rectTransform.pivot * rectTransform.rect.size;
		return (Vector2)A.localPosition - (vector - vector2);
	}

	public static Vector2 GetWorldPosition(this RectTransform A, Vector2 targetPivot)
	{
		Vector2 result = A.position;
		result.x += (targetPivot.x - A.pivot.x) * (A.rect.width * A.lossyScale.x);
		result.y += (targetPivot.y - A.pivot.y) * (A.rect.height * A.lossyScale.y);
		return result;
	}
}
