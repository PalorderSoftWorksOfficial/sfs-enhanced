using System;
using UnityEngine;

namespace SFS.UI;

public class FitBetween : MonoBehaviour
{
	[Serializable]
	public struct Provider
	{
		public RectTransform rectTransform;

		public bool useX;

		public bool useY;
	}

	public RectTransform[] left;

	public RectTransform[] right;

	public RectTransform[] top;

	public RectTransform[] bottom;

	public Vector2 ClampSize(Vector2 size)
	{
		float num = float.MinValue;
		float num2 = float.MaxValue;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		RectTransform[] array = left;
		foreach (RectTransform rectTransform in array)
		{
			if (rectTransform.gameObject.activeInHierarchy)
			{
				num = Mathf.Max(num, base.transform.parent.InverseTransformVector(rectTransform.GetWorldPosition(Vector2.one)).x);
			}
		}
		array = right;
		foreach (RectTransform rectTransform2 in array)
		{
			if (rectTransform2.gameObject.activeInHierarchy && rectTransform2.gameObject.activeInHierarchy)
			{
				num2 = Mathf.Min(num2, base.transform.parent.InverseTransformVector(rectTransform2.GetWorldPosition(Vector2.zero)).x);
			}
		}
		array = top;
		foreach (RectTransform rectTransform3 in array)
		{
			if (rectTransform3.gameObject.activeInHierarchy)
			{
				num3 = Mathf.Min(num3, base.transform.parent.InverseTransformVector(rectTransform3.GetWorldPosition(Vector2.zero)).x);
			}
		}
		array = bottom;
		foreach (RectTransform rectTransform4 in array)
		{
			if (rectTransform4.gameObject.activeInHierarchy)
			{
				num4 = Mathf.Max(num4, base.transform.parent.InverseTransformVector(rectTransform4.GetWorldPosition(Vector2.one)).x);
			}
		}
		Vector2 rhs = default(Vector2);
		if (num == float.MinValue || num2 == float.MaxValue)
		{
			rhs.x = float.MaxValue;
		}
		else
		{
			rhs.x = num2 - num;
		}
		if (num3 == float.MaxValue || num4 == float.MinValue)
		{
			rhs.y = float.MaxValue;
		}
		else
		{
			rhs.y = num3 - num4;
		}
		return Vector2.Min(size, rhs);
	}
}
