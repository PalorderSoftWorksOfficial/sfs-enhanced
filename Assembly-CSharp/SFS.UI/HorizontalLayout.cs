using System.Linq;
using UnityEngine;

namespace SFS.UI;

public class HorizontalLayout : LayoutGroup
{
	protected override void SetSize_Internal(Vector2 size)
	{
		size = LayoutUtility.GetApplySize(GetRectSize(), size, applySelfSizeX, applySelfSizeY);
		SetRectSize(size);
		Vector2 usableSizeForChildren = GetUsableSizeForChildren(size, RectTransform.Axis.Horizontal);
		if (spaceDistributionMode == SpaceDistributionMode.DistributeEvenly)
		{
			float x = usableSizeForChildren.x;
			NewElement[] activeElements = base.ActiveElements;
			for (int i = 0; i < activeElements.Length; i++)
			{
				activeElements[i].SetSize(new Vector2(x / (float)base.ActiveElements.Length, usableSizeForChildren.y));
			}
		}
		else if (spaceDistributionMode == SpaceDistributionMode.DistributeByUse)
		{
			float[] array = (from v in GetSizeDistribution(out var preferredCombinedSize)
				select v.x).ToArray();
			if (preferredCombinedSize.x < usableSizeForChildren.x)
			{
				float num = (usableSizeForChildren.x - preferredCombinedSize.x) / (float)base.ActiveElements.Length;
				for (int num2 = 0; num2 < base.ActiveElements.Length; num2++)
				{
					base.ActiveElements[num2].SetSize(new Vector2(preferredCombinedSize.x * array[num2] + num, usableSizeForChildren.y));
				}
			}
			else
			{
				for (int num3 = 0; num3 < base.ActiveElements.Length; num3++)
				{
					base.ActiveElements[num3].SetSize(new Vector2(usableSizeForChildren.x * array[num3], usableSizeForChildren.y));
				}
			}
		}
		else if (spaceDistributionMode == SpaceDistributionMode.TakeAvailable)
		{
			float num4 = usableSizeForChildren.x;
			NewElement[] activeElements = base.ActiveElements;
			foreach (NewElement newElement in activeElements)
			{
				float num5 = Mathf.Min(num4, newElement.GetPreferredSize().x);
				num4 -= num5;
				newElement.SetSize(new Vector2(num5, usableSizeForChildren.y));
			}
		}
		if (layoutGroup != null)
		{
			layoutGroup.enabled = false;
			layoutGroup.enabled = true;
		}
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		float x = (float)(base.Padding.left + base.Padding.right) + base.ActiveElements.Select((NewElement e) => e.GetPreferredSize().x + base.Spacing).Sum() - base.Spacing;
		float y = LayoutUtility.GetFilteredSize(GetRectSize(), base.ActiveElements.Select((NewElement e) => e.GetPreferredSize()), sizeMode).y + (float)base.Padding.vertical;
		return new Vector2(x, y);
	}
}
