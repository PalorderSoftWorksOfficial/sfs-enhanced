using System;
using SFS.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class TextInputMenu : Screen_Menu
{
	public TextInputElement elementPrefab;

	public TextAdapter cancelButtonText;

	public TextAdapter confirmButtonText;

	public RectTransform holder;

	public RectTransform elementsParent;

	private Action<string[]> confirmCallback;

	private CloseMode confirmCloseMode;

	private CloseMode cancelCloseMode;

	private TextInputElement[] elements = new TextInputElement[0];

	protected override CloseMode OnEscape => confirmCloseMode;

	public void Open(string cancelButtonText, string confirmButtonText, Action<string[]> confirmCallback, CloseMode closeMode, params TextInputElement[] elements)
	{
		Open(cancelButtonText, confirmButtonText, confirmCallback, closeMode, closeMode, elements);
	}

	public void Open(string cancelButtonText, string confirmButtonText, Action<string[]> confirmCallback, CloseMode confirmCloseMode, CloseMode cancelCloseMode, params TextInputElement[] elements)
	{
		TextInputElement[] array = this.elements;
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i].gameObject);
		}
		this.cancelButtonText.Text = cancelButtonText;
		this.confirmButtonText.Text = confirmButtonText;
		this.confirmCallback = confirmCallback;
		this.confirmCloseMode = confirmCloseMode;
		this.cancelCloseMode = cancelCloseMode;
		this.elements = elements;
		array = elements;
		foreach (TextInputElement obj in array)
		{
			obj.textBox.Holder = holder.gameObject;
			RectTransform component = obj.GetComponent<RectTransform>();
			component.SetParent(elementsParent);
			component.localScale = Vector3.one;
		}
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(elementsParent);
		Open();
		elements[0].textBox.Open();
	}

	public void Confirm()
	{
		CloseMenu(confirmCloseMode);
		confirmCallback(GetTextFromElements());
	}

	public void CancelTextInputMenu()
	{
		CloseMenu(cancelCloseMode);
	}

	public void CloseTextBox()
	{
		TextInputElement[] array = elements;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].textBox.Close();
		}
	}

	private void CloseMenu(CloseMode closeMode)
	{
		TextInputElement[] array = elements;
		for (int i = 0; i < array.Length; i++)
		{
			CloseMenu(array[i].textBox.GetScreenMenu());
		}
		CloseMenu(this);
		void CloseMenu(Screen_Menu screen)
		{
			if (!(screen == null))
			{
				if (closeMode == CloseMode.Current)
				{
					screen.Close();
				}
				else if (closeMode == CloseMode.Stack)
				{
					screen.CloseStack();
				}
			}
		}
	}

	private string[] GetTextFromElements()
	{
		string[] array = new string[elements.Length];
		for (int i = 0; i < elements.Length; i++)
		{
			array[i] = elements[i].textBox.Text;
		}
		return array;
	}

	public static TextInputElement Element(string titleText, string defaultText)
	{
		TextInputElement textInputElement = UnityEngine.Object.Instantiate(Menu.textInput.elementPrefab);
		if (string.IsNullOrWhiteSpace(titleText))
		{
			textInputElement.titleTextHolder.gameObject.SetActive(value: false);
		}
		else
		{
			textInputElement.titleText.Text = titleText;
		}
		textInputElement.textBox.Text = defaultText;
		return textInputElement;
	}

	public override void OnOpen()
	{
		holder.gameObject.SetActive(value: true);
	}

	public override void OnClose()
	{
		holder.gameObject.SetActive(value: false);
	}
}
