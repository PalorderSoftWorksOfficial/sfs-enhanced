using System;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ElementGenerator : MonoBehaviour
{
	public static ElementGenerator main;

	public NewElement defaultButton;

	public NewElement defaultText;

	private void Awake()
	{
		main = this;
	}

	public static MenuElement VerticalSpace(int space)
	{
		return delegate(GameObject root)
		{
			GameObject obj = new GameObject("Spacer");
			RectTransform rectTransform = obj.AddComponent<RectTransform>();
			obj.transform.SetParent(root.transform);
			rectTransform.sizeDelta = new Vector2(0f, space);
		};
	}

	public static MenuElement DefaultHorizontalGroup(params MenuElement[] elements)
	{
		return HorizontalGroup(delegate(HorizontalLayoutGroup group)
		{
			group.spacing = 10f;
			group.transform.localScale = new Vector3(1f, 1f, 1f);
		}, horizontalFit: true, verticalFit: true, elements);
	}

	public static MenuElement HorizontalGroup(Action<HorizontalLayoutGroup> onStart, bool horizontalFit, bool verticalFit, params MenuElement[] elements)
	{
		return delegate(GameObject root)
		{
			GameObject gameObject = new GameObject("Horizontal Group");
			gameObject.transform.SetParent(root.transform);
			HorizontalLayoutGroup horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
			RectTransform obj = (RectTransform)gameObject.transform;
			obj.pivot = new Vector2(0.5f, 1f);
			obj.localScale = Vector2.one;
			horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
			horizontalLayoutGroup.childControlHeight = false;
			horizontalLayoutGroup.childControlWidth = false;
			horizontalLayoutGroup.childForceExpandHeight = false;
			horizontalLayoutGroup.childForceExpandWidth = false;
			ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
			contentSizeFitter.horizontalFit = (horizontalFit ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained);
			contentSizeFitter.verticalFit = (verticalFit ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained);
			onStart(horizontalLayoutGroup);
			MenuElement[] array = elements;
			for (int i = 0; i < array.Length; i++)
			{
				array[i](gameObject);
			}
		};
	}
}
