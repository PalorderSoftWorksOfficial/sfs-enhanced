using System;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Translations;

public class FontSetter : MonoBehaviour
{
	private void Start()
	{
		Loc.OnChange += new Action(UpdateFont);
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(UpdateFont);
	}

	private void UpdateFont()
	{
		string text = Loc.main.Font;
		foreach (Font font in Base.language.fonts)
		{
			if (font.name == text)
			{
				SetFont(font);
				break;
			}
		}
	}

	private void SetFont(Font font)
	{
		if (GetComponent<Text>() != null)
		{
			Text component = GetComponent<Text>();
			component.font = font;
			component.SetLayoutDirty();
			component.rectTransform.ForceUpdateRectTransforms();
		}
		else if (GetComponentInChildren<Text>() != null)
		{
			Text componentInChildren = GetComponentInChildren<Text>();
			componentInChildren.font = font;
			componentInChildren.SetLayoutDirty();
			componentInChildren.rectTransform.ForceUpdateRectTransforms();
		}
		else if (GetComponentInChildren<TextMesh>() != null)
		{
			GetComponentInChildren<TextMesh>().font = font;
		}
	}
}
