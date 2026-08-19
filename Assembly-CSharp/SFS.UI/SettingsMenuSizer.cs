using UnityEngine;

namespace SFS.UI;

public class SettingsMenuSizer : NewElement
{
	public int minVerticalSize = 650;

	public int maxVerticalSize = 750;

	protected override Vector2 GetPreferredSize_Internal()
	{
		return Vector2.zero;
	}

	protected override void SetSize_Internal(Vector2 size)
	{
		Vector2 rectSize = new Vector2(size.x, Mathf.Clamp(size.y, minVerticalSize, maxVerticalSize));
		float num = ((size.y < (float)minVerticalSize) ? (size.y / (float)minVerticalSize) : 1f);
		SetRectSize(rectSize);
		base.transform.localScale = Vector3.one * num;
	}
}
