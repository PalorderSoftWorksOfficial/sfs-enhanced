using System;
using TranslucentImage;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI.ModGUI;

public class Box : GUIElement
{
	private global::TranslucentImage.TranslucentImage _backTranslucentImage;

	public Color Color
	{
		get
		{
			return _backTranslucentImage.color;
		}
		set
		{
			_backTranslucentImage.color = value;
		}
	}

	public float Opacity
	{
		get
		{
			return _backTranslucentImage.color.a;
		}
		set
		{
			_backTranslucentImage.color = new Color(_backTranslucentImage.color.r, _backTranslucentImage.color.g, _backTranslucentImage.color.b, value);
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		_backTranslucentImage = gameObject.transform.Find("Back (Game)").GetComponent<global::TranslucentImage.TranslucentImage>();
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
