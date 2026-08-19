using System;
using SFS.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Translations;

public class TranslationSelector : MonoBehaviour
{
	public TranslationVariable Selector = new TranslationVariable();

	private void Start()
	{
		Loc.OnChange += new Action(UpdateText);
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(UpdateText);
	}

	private void UpdateText()
	{
		string value = Selector.Field;
		SetText(GetComponent<Text>());
		SetText(GetComponentInChildren<Text>(includeInactive: true));
		TextMesh componentInChildren = GetComponentInChildren<TextMesh>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.text = value;
		}
		TextMeshProUGUI component = GetComponent<TextMeshProUGUI>();
		if (component != null)
		{
			component.text = value;
		}
		TextMeshProUGUI componentInChildren2 = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		if (componentInChildren2 != null)
		{
			componentInChildren2.text = value;
		}
		if (Application.isEditor)
		{
			GetComponent<ContainerElement>()?.UpdateHierarchy();
		}
		void SetText(Text text)
		{
			if (!(text == null))
			{
				text.text = value;
				text.SetLayoutDirty();
				text.rectTransform.ForceUpdateRectTransforms();
			}
		}
	}
}
