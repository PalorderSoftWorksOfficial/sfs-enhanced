using UnityEngine;

namespace SFS.UI;

public class ContainerElementWithScaling : ContainerElement
{
	[Space]
	public bool useScaleX_SetSize;

	public bool useScaleY_SetSize;

	public bool useScaleX_GetSize;

	public bool useScaleY_GetSize;

	protected override Vector2 GetPreferredSize_Internal()
	{
		Vector2 rectSize = GetRectSize();
		return ApplyInsideModifiers((child != null) ? LayoutUtility.GetApplySize(rectSize, child.GetPreferredSize(), applySizeX, applySizeY) : rectSize, 1) * new Vector2(useScaleX_GetSize ? base.transform.localScale.x : 1f, useScaleY_GetSize ? base.transform.localScale.y : 1f);
	}

	protected override void SetSize_Internal(Vector2 availableSize)
	{
		availableSize /= new Vector2(useScaleX_SetSize ? base.transform.localScale.x : 1f, useScaleY_SetSize ? base.transform.localScale.y : 1f);
		base.SetSize_Internal(availableSize);
	}
}
