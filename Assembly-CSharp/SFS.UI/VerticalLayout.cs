using System.Linq;
using UnityEngine;

namespace SFS.UI;

public class VerticalLayout : LayoutGroup
{
	protected override void SetSize_Internal(Vector2 size)
	{
		size = LayoutUtility.GetApplySize(GetRectSize(), size, applySelfSizeX, applySelfSizeY);
		SetRectSize(size);
		Vector2 usableSizeForChildren = GetUsableSizeForChildren(size, RectTransform.Axis.Vertical);
		if (spaceDistributionMode == SpaceDistributionMode.DistributeEvenly)
		{
			float y = usableSizeForChildren.y;
			NewElement[] activeElements = base.ActiveElements;
			for (int i = 0; i < activeElements.Length; i++)
			{
				activeElements[i].SetSize(new Vector2(usableSizeForChildren.x, y / (float)base.ActiveElements.Length));
			}
		}
		else if (spaceDistributionMode == SpaceDistributionMode.DistributeByUse)
		{
			float[] array = (from d in GetSizeDistribution(out var preferredCombinedSize)
				select d.y).ToArray();
			for (int num = 0; num < base.ActiveElements.Length; num++)
			{
				base.ActiveElements[num].SetSize(new Vector2(usableSizeForChildren.x, usableSizeForChildren.y * array[num]));
			}
			if (preferredCombinedSize.y < usableSizeForChildren.y)
			{
				float num2 = (usableSizeForChildren.y - preferredCombinedSize.y) / (float)base.ActiveElements.Length;
				for (int num3 = 0; num3 < base.ActiveElements.Length; num3++)
				{
					base.ActiveElements[num3].SetSize(new Vector2(usableSizeForChildren.x, preferredCombinedSize.y * array[num3] + num2));
				}
			}
			else
			{
				for (int num4 = 0; num4 < base.ActiveElements.Length; num4++)
				{
					base.ActiveElements[num4].SetSize(new Vector2(usableSizeForChildren.x, usableSizeForChildren.y * array[num4]));
				}
			}
		}
		else if (spaceDistributionMode == SpaceDistributionMode.TakeAvailable)
		{
			float num5 = usableSizeForChildren.y;
			NewElement[] activeElements = base.ActiveElements;
			foreach (NewElement newElement in activeElements)
			{
				float num6 = Mathf.Min(num5, newElement.GetPreferredSize().y);
				num5 -= num6;
				newElement.SetSize(new Vector2(usableSizeForChildren.x, num6));
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
		float x = LayoutUtility.GetFilteredSize(GetRectSize(), base.ActiveElements.Select((NewElement e) => e.GetPreferredSize()), sizeMode).x + (float)base.Padding.horizontal;
		float y = (float)base.Padding.vertical + base.ActiveElements.Sum((NewElement e) => e.GetPreferredSize().y + base.Spacing) - base.Spacing;
		return new Vector2(x, y);
	}
}
