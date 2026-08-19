using System;
using System.Collections.Generic;
using System.Text;
using SFS.IO;
using SFS.UI;
using TMPro;
using UnityEngine;

namespace SFS.Input;

public class KeybindingsPC : SettingsBase<KeybindingsPC.Data>
{
	public class Data
	{
		public Key Close_Menu = KeyCode.Escape;

		public Key[] SaveLoad = new Key[2]
		{
			KeyCode.F5,
			KeyCode.F9
		};

		public Key Select_All = Key.Ctrl_(KeyCode.A);

		public Key[] CopyPaste = new Key[2]
		{
			Key.Ctrl_(KeyCode.C),
			Key.Ctrl_(KeyCode.V)
		};

		public Key Duplicate = Key.Ctrl_(KeyCode.D);

		public Key Delete = KeyCode.Delete;

		public Key Undo = Key.Ctrl_(KeyCode.Z);

		public Key Redo = Key.Ctrl_(KeyCode.Y);

		public Key[] Rotate_Part = new Key[2]
		{
			KeyCode.Q,
			KeyCode.E
		};

		public Key[] Flip_Part = new Key[4]
		{
			KeyCode.W,
			KeyCode.A,
			KeyCode.S,
			KeyCode.D
		};

		public Key Toggle_Ignition = KeyCode.Space;

		public Key[] Throttle = new Key[2]
		{
			KeyCode.LeftControl,
			KeyCode.LeftShift
		};

		public Key[] MinMax_Throttle = new Key[2]
		{
			KeyCode.X,
			KeyCode.Z
		};

		public Key Toggle_RCS = KeyCode.R;

		public Key[] Turn_Rocket = new Key[2]
		{
			KeyCode.Q,
			KeyCode.E
		};

		public Key[] Move_Rocket_Using_RCS = new Key[4]
		{
			KeyCode.W,
			KeyCode.A,
			KeyCode.S,
			KeyCode.D
		};

		public Key Activate_Stage = KeyCode.Return;

		public Key Toggle_Map = KeyCode.M;

		public Key[] Timewarp = new Key[2]
		{
			KeyCode.Comma,
			KeyCode.Period
		};

		public Key[] Switch_Rocket = new Key[2]
		{
			KeyCode.LeftBracket,
			KeyCode.RightBracket
		};

		public Key Toggle_Console = KeyCode.F1;
	}

	[Serializable]
	public class Key : I_Key
	{
		public bool ctrl;

		public KeyCode key;

		private bool HoldingControl => ctrl == UnityEngine.Input.GetKey(KeyCode.LeftControl);

		public static Key Ctrl_(KeyCode key)
		{
			return new Key
			{
				key = key,
				ctrl = true
			};
		}

		public static implicit operator Key(KeyCode key)
		{
			return new Key
			{
				key = key,
				ctrl = false
			};
		}

		bool I_Key.IsKeyDown()
		{
			if (UnityEngine.Input.GetKeyDown(key))
			{
				return HoldingControl;
			}
			return false;
		}

		bool I_Key.IsKeyStay()
		{
			return UnityEngine.Input.GetKey(key);
		}

		bool I_Key.IsKeyUp()
		{
			return UnityEngine.Input.GetKeyUp(key);
		}
	}

	public static KeybindingsPC main;

	public static Data keys = new Data();

	public GameObject keybindingPrefab;

	public GameObject spacePrefab;

	public GameObject textPrefab;

	public Transform keybindingsHolder;

	private List<KeyBinder> elements = new List<KeyBinder>();

	protected override string FileName => "Keybindings";

	private void Awake()
	{
		main = this;
		Load();
		Data data = new Data();
		Create(keys.SaveLoad, data.SaveLoad, "Save/Load");
		CreateSpace();
		CreateSpace();
		CreateSpace();
		Create(keys.Select_All, data.Select_All, "Select_All");
		Create(keys.CopyPaste, data.CopyPaste, "Copy/Paste");
		Create(keys.Duplicate, data.Duplicate, "Duplicate");
		Create(keys.Delete, data.Delete, "Delete");
		CreateSpace();
		Create(keys.Rotate_Part, data.Rotate_Part, "Rotate_Part");
		Create(keys.Flip_Part, data.Flip_Part, "Flip_Part");
		CreateSpace();
		Create(keys.Undo, data.Undo, "Undo");
		Create(keys.Redo, data.Redo, "Redo");
		CreateSpace();
		CreateSpace();
		CreateSpace();
		Create(keys.Toggle_Ignition, data.Toggle_Ignition, "Toggle_Ignition");
		Create(keys.Throttle, data.Throttle, "Adjust throttle");
		Create(keys.MinMax_Throttle, data.MinMax_Throttle, "Min/Max throttle");
		CreateSpace();
		Create(keys.Turn_Rocket, data.Turn_Rocket, "Turn_Rocket");
		CreateSpace();
		Create(keys.Toggle_RCS, data.Toggle_RCS, "Toggle RCS");
		Create(keys.Move_Rocket_Using_RCS, data.Move_Rocket_Using_RCS, "Move using RCS");
		CreateSpace();
		Create(keys.Activate_Stage, data.Activate_Stage, "Activate_Stage");
		CreateSpace();
		Create(keys.Toggle_Map, data.Toggle_Map, "Toggle_Map");
		CreateSpace();
		Create(keys.Timewarp, data.Timewarp, "Timewarp");
		CreateSpace();
		Create(keys.Switch_Rocket, data.Switch_Rocket, "Switch_Rocket");
		CreateSpace();
		CreateSpace();
		CreateSpace();
		Create(keys.Toggle_Console, data.Toggle_Console, "Toggle_Console");
	}

	private void Create(Key key, Key defaultKey, string name)
	{
		Create(new Key[1] { key }, new Key[1] { defaultKey }, name);
	}

	private void Create(Key[] keys, Key[] defaultKeys, string name)
	{
		Create(keys, defaultKeys, name, base.Save);
	}

	public void Create(Key[] keys, Key[] defaultKeys, string name, Action apply)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(keybindingPrefab, keybindingsHolder);
		if (name.Contains("_"))
		{
			StringBuilder stringBuilder = new StringBuilder(name[0].ToString());
			for (int i = 1; i < name.Length; i++)
			{
				stringBuilder.Append(name[i].ToString().ToLower());
			}
			gameObject.GetComponentInChildren<TMP_Text>().text = stringBuilder.Replace("_", " ").ToString();
		}
		else
		{
			gameObject.GetComponentInChildren<TMP_Text>().text = name;
		}
		List<KeyBinder> list = new List<KeyBinder> { gameObject.GetComponentInChildren<KeyBinder>() };
		int num = 0;
		while (list.Count < keys.Length && num++ < 20)
		{
			list.Add(UnityEngine.Object.Instantiate(list[0], gameObject.transform));
		}
		for (int j = 0; j < keys.Length; j++)
		{
			Key key = keys[j];
			Key defaultKey = defaultKeys[j];
			list[j].Initialize(Menu.settings.menuHolder, key, defaultKey, apply);
		}
		elements.AddRange(list);
	}

	public void CreateText(string text)
	{
		UnityEngine.Object.Instantiate(textPrefab, keybindingsHolder).GetComponentInChildren<TMP_Text>().text = text;
	}

	public void CreateSpace()
	{
		UnityEngine.Object.Instantiate(spacePrefab, keybindingsHolder);
	}

	public void ResetKeybindings()
	{
		foreach (KeyBinder element in elements)
		{
			element.ResetToDefault();
		}
	}

	protected override void OnLoad()
	{
		keys = settings;
	}
}
