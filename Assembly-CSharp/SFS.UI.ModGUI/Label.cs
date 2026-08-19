using TMPro;
using UnityEngine;

namespace SFS.UI.ModGUI;

public class Label : GUIElement
{
	private TextMeshProUGUI textAdapter;

	public string Text
	{
		get
		{
			return textAdapter.text;
		}
		set
		{
			textAdapter.text = value;
		}
	}

	public Color Color
	{
		get
		{
			return textAdapter.color;
		}
		set
		{
			textAdapter.color = value;
		}
	}

	public float Opacity
	{
		get
		{
			return textAdapter.color.a;
		}
		set
		{
			textAdapter.color = new Color(textAdapter.color.r, textAdapter.color.g, textAdapter.color.b, value);
		}
	}

	public TextAlignmentOptions TextAlignment
	{
		get
		{
			return textAdapter.alignment;
		}
		set
		{
			textAdapter.alignment = value;
		}
	}

	public bool AutoFontResize
	{
		get
		{
			return textAdapter.enableAutoSizing;
		}
		set
		{
			textAdapter.enableAutoSizing = value;
		}
	}

	public float FontSize
	{
		get
		{
			return textAdapter.fontSize;
		}
		set
		{
			textAdapter.fontSize = value;
		}
	}

	public FontStyles FontStyle
	{
		get
		{
			return textAdapter.fontStyle;
		}
		set
		{
			textAdapter.fontStyle = value;
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		textAdapter = gameObject.GetComponent<TextMeshProUGUI>();
	}
}
