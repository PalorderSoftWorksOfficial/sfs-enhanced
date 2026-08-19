using UnityEngine.UI;

public static class LayoutUtility
{
	public static void DisableChildControl(this HorizontalOrVerticalLayoutGroup group)
	{
		group.childControlHeight = false;
		group.childControlWidth = false;
		group.childScaleHeight = false;
		group.childScaleWidth = false;
		group.childForceExpandHeight = false;
		group.childForceExpandWidth = false;
	}
}
