using UnityEngine;

namespace SFS.UI;

public class ContainerElement : NewElement
{
	public NewElement child;

	public bool applySizeX;

	public bool applySizeY;

	private void Reset()
	{
		foreach (Transform item in base.transform)
		{
			child = item.GetComponent<NewElement>();
			if (child != null)
			{
				break;
			}
		}
	}

	private void Awake()
	{
		if (child != null)
		{
			child.SetParent(this);
		}
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		Vector2 rectSize = GetRectSize();
		return ApplyInsideModifiers((child != null) ? LayoutUtility.GetApplySize(rectSize, child.GetPreferredSize(), applySizeX, applySizeY) : rectSize, 1);
	}

	protected override void SetSize_Internal(Vector2 availableSize)
	{
		Vector2 applySize = LayoutUtility.GetApplySize(GetRectSize(), availableSize, applySizeX, applySizeY);
		SetRectSize(applySize);
		if (child != null)
		{
			child.SetSize(ApplyInsideModifiers(applySize, -1));
		}
	}

	public override void ActAsRoot()
	{
		SetSize(GetPreferredSize());
	}
}
