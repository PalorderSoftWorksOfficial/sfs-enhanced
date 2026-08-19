using TMPro;
using TranslucentImage;
using UnityEngine;
using UnityEngine.Events;

namespace SFS.UI.ModGUI;

public class TextInput : GUIElement
{
	private global::TranslucentImage.TranslucentImage _backTranslucentImage;

	private UnityAction<string> _onChange;

	public TMP_InputField field;

	public Color FieldColor
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

	public float FieldOpacity
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

	public string Text
	{
		get
		{
			return field.text;
		}
		set
		{
			field.text = value;
		}
	}

	public UnityAction<string> OnChange
	{
		get
		{
			return _onChange;
		}
		set
		{
			if (_onChange != null)
			{
				field.onValueChanged.RemoveListener(_onChange);
			}
			_onChange = value;
			field.onValueChanged.AddListener(_onChange);
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		field = gameObject.GetComponentInChildren<TMP_InputField>();
		_backTranslucentImage = gameObject.transform.Find("Text Box").Find("BackTranslucent").GetComponent<global::TranslucentImage.TranslucentImage>();
	}
}
