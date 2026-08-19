using SFS.UI;
using UnityEngine;

namespace SFS.Input;

[RequireComponent(typeof(RectTransform))]
public class InputMask2D : MonoBehaviour
{
	private RectTransform rectTransform;

	public bool Pointcast(Vector2 canvasPosition)
	{
		return rectTransform.rect.Contains(base.transform.InverseTransformPoint(canvasPosition));
	}

	private void Awake()
	{
		rectTransform = base.transform.Rect();
	}
}
