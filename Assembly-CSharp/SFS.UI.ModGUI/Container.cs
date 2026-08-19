using System;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI.ModGUI;

public class Container : GUIElement
{
	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
	}

	public HorizontalOrVerticalLayoutGroup CreateLayoutGroup(Type type, TextAnchor childAlignment = TextAnchor.MiddleCenter, float spacing = 20f, RectOffset padding = null, bool disableChildSizeControl = true)
	{
		HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup = type switch
		{
			Type.Vertical => gameObject.GetOrAddComponent<VerticalLayoutGroup>(), 
			Type.Horizontal => gameObject.GetOrAddComponent<HorizontalLayoutGroup>(), 
			_ => throw new ArgumentOutOfRangeException("type", type, null), 
		};
		horizontalOrVerticalLayoutGroup.childAlignment = childAlignment;
		horizontalOrVerticalLayoutGroup.spacing = spacing;
		if (disableChildSizeControl)
		{
			horizontalOrVerticalLayoutGroup.DisableChildControl();
		}
		if (padding != null)
		{
			horizontalOrVerticalLayoutGroup.padding = padding;
		}
		return horizontalOrVerticalLayoutGroup;
	}
}
