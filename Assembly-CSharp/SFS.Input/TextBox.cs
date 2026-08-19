using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Input;

[RequireComponent(typeof(Text))]
public class TextBox : Screen_Menu
{
	public GameObject holder;

	public Text textField;

	public bool toggleHolderActive = true;

	public bool multiline;

	public int characterLimit = 1000;

	public int cursorPosition;

	public StringBuilder text;

	public Action<string> onInputFinished;

	private TouchScreenKeyboard keyboard;

	public string Text
	{
		get
		{
			if (text == null)
			{
				return textField.text;
			}
			return text.ToString();
		}
		set
		{
			text = new StringBuilder(value ?? "");
			textField.text = value ?? "";
		}
	}

	protected override CloseMode OnEscape => CloseMode.None;

	private void Process_Keyboard()
	{
		string text = UnityEngine.Input.inputString.Replace("\n", "").Replace("\r", "").Replace("\b", "");
		if (text.Length > 0)
		{
			this.text.Insert(cursorPosition, text);
			cursorPosition += text.Length;
		}
		if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace) && cursorPosition > 0)
		{
			StopCoroutine("HoldingBackspace");
			this.text.Remove(cursorPosition - 1, 1);
			cursorPosition--;
			StartCoroutine("HoldingBackspace");
		}
		if (UnityEngine.Input.GetKeyUp(KeyCode.Backspace))
		{
			StopCoroutine("HoldingBackspace");
		}
		if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) && cursorPosition > 0)
		{
			StopCoroutine("HoldingLeftArrow");
			cursorPosition--;
			StartCoroutine("HoldingLeftArrow");
		}
		if (UnityEngine.Input.GetKeyUp(KeyCode.LeftArrow))
		{
			StopCoroutine("HoldingLeftArrow");
		}
		if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) && cursorPosition < this.text.Length)
		{
			StopCoroutine("HoldingRightArrow");
			cursorPosition++;
			StartCoroutine("HoldingRightArrow");
		}
		if (UnityEngine.Input.GetKeyUp(KeyCode.RightArrow))
		{
			StopCoroutine("HoldingRightArrow");
		}
		if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
		{
			if (!multiline)
			{
				Close();
				return;
			}
			this.text.Append("\n");
			cursorPosition++;
		}
		this.text = ClampToCharacterLimit(this.text);
		textField.text = GetDisplayText(useBlink: true);
	}

	private IEnumerator HoldingBackspace()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		while (cursorPosition > 0)
		{
			text.Remove(cursorPosition - 1, 1);
			cursorPosition--;
			yield return new WaitForSecondsRealtime(0.05f);
		}
	}

	private IEnumerator HoldingLeftArrow()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		while (cursorPosition > 0)
		{
			cursorPosition--;
			yield return new WaitForSecondsRealtime(0.05f);
		}
	}

	private IEnumerator HoldingRightArrow()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		while (cursorPosition < text.Length)
		{
			cursorPosition++;
			yield return new WaitForSecondsRealtime(0.05f);
		}
	}

	private void Process_TouchKeyboard()
	{
		if (keyboard.status != TouchScreenKeyboard.Status.Visible)
		{
			Close();
			return;
		}
		text = ClampToCharacterLimit(new StringBuilder(keyboard.text));
		textField.text = GetDisplayText(useBlink: false);
	}

	private string GetDisplayText(bool useBlink)
	{
		StringBuilder stringBuilder = new StringBuilder(text.ToString());
		if (cursorPosition > stringBuilder.Length)
		{
			cursorPosition = stringBuilder.Length;
		}
		if (useBlink)
		{
			stringBuilder.Insert(cursorPosition, ((double)Time.unscaledTime % 0.8 > 0.4) ? "|" : "<color=#ffffff00>|</color>");
		}
		return stringBuilder.ToString();
	}

	private StringBuilder ClampToCharacterLimit(StringBuilder text)
	{
		if (text.Length <= characterLimit)
		{
			return text;
		}
		return new StringBuilder(text.ToString().Substring(0, characterLimit));
	}

	public override void ProcessInput()
	{
		Process_Keyboard();
	}

	public override void Open()
	{
		if (ScreenManager.main.CurrentScreen is TextBox)
		{
			ScreenManager.main.CloseCurrent();
		}
		base.Open();
	}

	public override void OnOpen()
	{
		if (toggleHolderActive)
		{
			holder.SetActive(value: true);
		}
		textField.supportRichText = true;
		text = new StringBuilder(textField.text);
		cursorPosition = text.Length;
		keyboard = TouchScreenKeyboard.Open(text.ToString(), TouchScreenKeyboardType.NamePhonePad, autocorrection: false, multiline, secure: false, alert: true, "", 0);
	}

	public override void OnClose()
	{
		if (toggleHolderActive)
		{
			holder.SetActive(value: false);
		}
		string text = this.text.ToString();
		if (text != textField.text)
		{
			onInputFinished?.Invoke(text);
		}
		textField.text = text;
		this.text = null;
		keyboard = null;
	}
}
