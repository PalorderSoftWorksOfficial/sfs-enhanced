using System;
using TranslucentImage;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI.ModGUI;

public class Window : GUIElement
{
	public int ID;

	private global::TranslucentImage.TranslucentImage backTranslucentImage;

	private DraggableWindowModule draggableWindowModule;

	private TextAdapter titleTextAdapter;

	private Transform childrenHolder;

	public string Title
	{
		get
		{
			return titleTextAdapter.Text;
		}
		set
		{
			titleTextAdapter.Text = value;
		}
	}

	public Color TitleColor
	{
		get
		{
			return titleTextAdapter.Color;
		}
		set
		{
			titleTextAdapter.Color = value;
		}
	}

	public float TitleOpacity
	{
		get
		{
			return titleTextAdapter.Color.a;
		}
		set
		{
			titleTextAdapter.Color = new Color(titleTextAdapter.Color.r, titleTextAdapter.Color.g, titleTextAdapter.Color.b, value);
		}
	}

	public Color WindowColor
	{
		get
		{
			return backTranslucentImage.color;
		}
		set
		{
			backTranslucentImage.color = value;
		}
	}

	public float WindowOpacity
	{
		get
		{
			return backTranslucentImage.color.a;
		}
		set
		{
			backTranslucentImage.color = new Color(backTranslucentImage.color.r, backTranslucentImage.color.g, backTranslucentImage.color.b, value);
		}
	}

	public bool Draggable
	{
		get
		{
			return draggableWindowModule.draggable;
		}
		set
		{
			draggableWindowModule.draggable = value;
		}
	}

	public Transform ChildrenHolder => childrenHolder ?? (childrenHolder = gameObject.transform.Find("Mask").Find("Children Holder"));

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		draggableWindowModule = gameObject.GetComponent<DraggableWindowModule>();
		titleTextAdapter = gameObject.transform.Find("Title").GetComponent<TextAdapter>();
		backTranslucentImage = gameObject.transform.Find("Back (Game)").GetComponent<global::TranslucentImage.TranslucentImage>();
	}

	public HorizontalOrVerticalLayoutGroup CreateLayoutGroup(Type type, TextAnchor childAlignment = TextAnchor.MiddleCenter, float spacing = 20f, RectOffset padding = null, bool disableChildSizeControl = true)
	{
		HorizontalOrVerticalLayoutGroup horizontalOrVerticalLayoutGroup = type switch
		{
			Type.Vertical => ChildrenHolder.GetOrAddComponent<VerticalLayoutGroup>(), 
			Type.Horizontal => ChildrenHolder.GetOrAddComponent<HorizontalLayoutGroup>(), 
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

	public void EnableScrolling(Type type)
	{
		switch (type)
		{
		case Type.Vertical:
			ChildrenHolder.GetComponent<ScrollElement>().vertical = true;
			break;
		case Type.Horizontal:
			ChildrenHolder.GetComponent<ScrollElement>().horizontal = true;
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}
}
