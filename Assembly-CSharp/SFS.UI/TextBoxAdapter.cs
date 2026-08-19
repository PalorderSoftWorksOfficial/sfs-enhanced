using SFS.Input;
using SFS.Tween;
using TMPro;
using UnityEngine;

namespace SFS.UI;

public class TextBoxAdapter : MonoBehaviour
{
	private TextBox mobileTextBox;

	private TMP_InputField TMProTextBox;

	private bool isInit;

	public string Text
	{
		get
		{
			Init();
			if (mobileTextBox != null)
			{
				return mobileTextBox.Text;
			}
			if (TMProTextBox != null)
			{
				return TMProTextBox.text;
			}
			return null;
		}
		set
		{
			Init();
			if (mobileTextBox != null)
			{
				mobileTextBox.Text = value;
			}
			if (TMProTextBox != null)
			{
				TMProTextBox.text = value;
			}
		}
	}

	public GameObject Holder
	{
		set
		{
			Init();
			if (mobileTextBox != null)
			{
				mobileTextBox.holder = value;
			}
		}
	}

	public Screen_Menu GetScreenMenu()
	{
		return mobileTextBox;
	}

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		if (!isInit)
		{
			mobileTextBox = GetComponent<TextBox>();
			TMProTextBox = GetComponent<TMP_InputField>();
			isInit = true;
		}
	}

	public void Open()
	{
		if (mobileTextBox != null)
		{
			mobileTextBox.Open();
		}
		if (TMProTextBox != null)
		{
			TweenManager.DelayCall(0.1f, delegate
			{
				TMProTextBox.Select();
			});
		}
	}

	public void Close()
	{
		if (mobileTextBox != null)
		{
			mobileTextBox.Close();
		}
	}
}
