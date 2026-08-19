using System;
using SFS.UI;
using TMPro;
using UnityEngine;

namespace SFS.Input;

public class KeyBinder : Screen_Base
{
	public ButtonPC button;

	public TMP_Text text;

	private GameObject holder;

	private KeybindingsPC.Key key;

	private Action apply;

	private KeybindingsPC.Key defaultKey;

	public override bool PauseWhileOpen => true;

	public void Initialize(GameObject holder, KeybindingsPC.Key key, KeybindingsPC.Key defaultKey, Action apply)
	{
		this.holder = holder;
		this.apply = apply;
		this.key = key;
		text.text = GetDisplayName(key);
		button.onClick += (Action)delegate
		{
			ScreenManager.main.OpenScreen(() => this);
		};
		this.defaultKey = defaultKey;
	}

	public override void OnOpen()
	{
		text.text = "-";
		holder.SetActive(value: true);
	}

	public override void OnClose()
	{
		holder.SetActive(value: false);
	}

	public override void ProcessInput()
	{
		if (!UnityEngine.Input.anyKeyDown && !UnityEngine.Input.GetKeyUp(KeyCode.LeftControl))
		{
			return;
		}
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (((UnityEngine.Input.GetKeyDown(value) && value != KeyCode.LeftControl) || UnityEngine.Input.GetKeyUp(KeyCode.LeftControl)) && Array.IndexOf(new KeyCode[3]
			{
				KeyCode.Escape,
				KeyCode.Mouse0,
				KeyCode.Mouse1
			}, value) == -1)
			{
				key.ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl);
				key.key = (UnityEngine.Input.GetKeyUp(KeyCode.LeftControl) ? KeyCode.LeftControl : value);
				apply();
				text.text = GetDisplayName(key);
				ScreenManager.main.CloseCurrent();
				break;
			}
		}
	}

	private static string GetDisplayName(KeybindingsPC.Key k)
	{
		return (k.ctrl ? "Ctrl + " : "") + GetString();
		string GetString()
		{
			return k.key switch
			{
				KeyCode.UpArrow => "Up", 
				KeyCode.DownArrow => "Down", 
				KeyCode.LeftArrow => "Left", 
				KeyCode.RightArrow => "Right", 
				KeyCode.LeftControl => "Ctrl", 
				KeyCode.RightControl => "Ctrl", 
				KeyCode.LeftShift => "Shift", 
				KeyCode.RightShift => "Shift", 
				KeyCode.Period => "<", 
				KeyCode.Comma => ">", 
				KeyCode.LeftBracket => "[", 
				KeyCode.RightBracket => "]", 
				KeyCode.Return => "Enter", 
				KeyCode.KeypadEnter => "Enter", 
				_ => k.key.ToString(), 
			};
		}
	}

	public void ResetToDefault()
	{
		key.ctrl = defaultKey.ctrl;
		key.key = defaultKey.key;
		apply();
		text.text = GetDisplayName(key);
	}
}
