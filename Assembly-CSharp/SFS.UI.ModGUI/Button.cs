using System;
using UnityEngine;

namespace SFS.UI.ModGUI;

public class Button : GUIElement
{
	private ButtonPC _button;

	private Action _onClick;

	private TextAdapter _textAdapter;

	public string Text
	{
		get
		{
			return _textAdapter.Text;
		}
		set
		{
			_textAdapter.Text = value;
		}
	}

	public Color TextColor
	{
		get
		{
			return _textAdapter.Color;
		}
		set
		{
			_textAdapter.Color = value;
		}
	}

	public float TextOpacity
	{
		get
		{
			return _textAdapter.Color.a;
		}
		set
		{
			_textAdapter.Color = new Color(_textAdapter.Color.r, _textAdapter.Color.g, _textAdapter.Color.b, value);
		}
	}

	public Action OnClick
	{
		get
		{
			return _onClick;
		}
		set
		{
			if (_onClick != null)
			{
				_button.onClick -= _onClick;
			}
			_onClick = value;
			_button.onClick += value;
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		_button = gameObject.GetComponent<ButtonPC>();
		_textAdapter = gameObject.transform.Find("Text").GetComponent<TextAdapter>();
	}
}
