using SFS.UI;
using UnityEngine;

namespace SFS.Builds;

public class PickGridIcon : MonoBehaviour
{
	public RectTransform rectTransform;

	public Button button;

	[HideInInspector]
	public Vector2 aspectRatio;

	private void OnRectTransformDimensionsChange()
	{
		if (Mathf.Abs(rectTransform.rect.height - rectTransform.rect.width / aspectRatio.x * aspectRatio.y) > 0.1f)
		{
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.width / aspectRatio.x * aspectRatio.y);
		}
	}
}
