using System;
using SFS.Builds;
using SFS.Input;
using SFS.IO;
using SFS.Parsers.Json;
using SFS.World;
using UnityEngine;

namespace ModLoader;

public abstract class ModKeybindings
{
	private Mod mod;

	protected static T SetupKeybindings<T>(Mod mod) where T : ModKeybindings, new()
	{
		T val = Load<T>(mod);
		val.mod = mod;
		val.CreateUI();
		return val;
	}

	public abstract void CreateUI();

	public void CreateUI_Keybinding(KeybindingsPC.Key key, KeybindingsPC.Key defaultKey, string displayName)
	{
		CreateUI_Keybinding(new KeybindingsPC.Key[1] { key }, new KeybindingsPC.Key[1] { defaultKey }, displayName);
	}

	public void CreateUI_Keybinding(KeybindingsPC.Key[] keys, KeybindingsPC.Key[] defaultKeys, string displayName)
	{
		KeybindingsPC.main.Create(keys, defaultKeys, displayName, Save);
	}

	public void CreateUI_Text(string text)
	{
		KeybindingsPC.main.CreateText(text);
	}

	public void CreateUI_Space()
	{
		KeybindingsPC.main.CreateSpace();
	}

	public static void AddOnKeyDown_Build(I_Key key, Action onKeyDown)
	{
		if (BuildManager.main != null)
		{
			BuildManager.main.build_Input.keysNode.AddOnKeyDown(key, onKeyDown);
		}
		else
		{
			Debug.LogError("This method only works in Builder Scene");
		}
	}

	public static void AddOnKeyDown_World(I_Key key, Action onKeyDown)
	{
		if (GameManager.main != null)
		{
			GameManager.AddOnKeyDown(key, onKeyDown);
		}
		else
		{
			Debug.LogError("This method only works in World Scene");
		}
	}

	public static void AddOnKeyDown(I_Key key, Action onKeyDown)
	{
		Screen_Base.AddOnKeyDown(key, onKeyDown);
	}

	private void Save()
	{
		string text = JsonWrapper.ToJson(this, pretty: true);
		GetPath(mod).WriteText(text);
	}

	private static T Load<T>(Mod mod) where T : ModKeybindings, new()
	{
		FilePath path = GetPath(mod);
		if (!path.FileExists())
		{
			return new T
			{
				mod = mod
			};
		}
		T obj = JsonWrapper.FromJson<T>(path.ReadText()) ?? new T();
		obj.mod = mod;
		return obj;
	}

	private static FilePath GetPath(Mod mod)
	{
		return new FolderPath(mod.ModFolder).ExtendToFile("keybindings.txt");
	}
}
