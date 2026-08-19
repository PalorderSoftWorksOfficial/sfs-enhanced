using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SFS.UI;

public class NewSizeSyncGroup : NewElement
{
	public SizeMode horizontalSync;

	public SizeMode verticalSync;

	[Space]
	public bool applySizeX;

	public bool applySizeY;

	[Space]
	public List<NewElement> children = new List<NewElement>();

	private void Awake()
	{
		foreach (NewElement child in children)
		{
			child.SetParent(this);
		}
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		Vector2 rectSize = GetRectSize();
		float x = LayoutUtility.GetFilteredSize(rectSize, from e in children
			where e.isActiveAndEnabled
			select e.GetPreferredSize(), horizontalSync).x;
		float y = LayoutUtility.GetFilteredSize(rectSize, from e in children
			where e.isActiveAndEnabled
			select e.GetPreferredSize(), verticalSync).y;
		return ApplyInsideModifiers(new Vector2(x, y), 1);
	}

	protected override void SetSize_Internal(Vector2 availableSize)
	{
		availableSize = LayoutUtility.GetApplySize(GetRectSize(), availableSize, applySizeX, applySizeY);
		SetRectSize(availableSize);
		Vector2 size = ApplyInsideModifiers(availableSize, -1);
		foreach (NewElement child in children)
		{
			child.SetSize(size);
		}
	}

	public override void ActAsRoot()
	{
		SetSize(GetPreferredSize());
	}

	private void GetElements()
	{
		children = (from e in GetComponentsInChildren<NewElement>()
			where e.transform.parent == base.transform
			select e).ToList();
	}
}
