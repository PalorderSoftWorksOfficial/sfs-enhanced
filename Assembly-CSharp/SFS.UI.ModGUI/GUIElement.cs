using UnityEngine;

namespace SFS.UI.ModGUI;

public abstract class GUIElement
{
	public GameObject gameObject;

	public RectTransform rectTransform;

	public virtual Vector2 Size
	{
		get
		{
			return rectTransform.sizeDelta;
		}
		set
		{
			rectTransform.sizeDelta = value;
		}
	}

	public virtual Vector2 Position
	{
		get
		{
			return rectTransform.localPosition;
		}
		set
		{
			rectTransform.localPosition = value;
		}
	}

	public virtual bool Active
	{
		get
		{
			return gameObject.activeSelf;
		}
		set
		{
			gameObject.SetActive(value);
		}
	}

	public abstract void Init(GameObject self, Transform parent);

	public static implicit operator Transform(GUIElement element)
	{
		if (!(element is Window window))
		{
			return element.rectTransform;
		}
		return window.ChildrenHolder;
	}
}
