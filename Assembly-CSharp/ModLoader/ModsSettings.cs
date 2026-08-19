using System;
using System.Collections.Generic;
using SFS.IO;
using UnityEngine;

namespace ModLoader;

public class ModsSettings : SettingsBase<ModsSettings.Data>
{
	[Serializable]
	public class Data
	{
		public Dictionary<string, bool> modsActive = new Dictionary<string, bool>();

		public Dictionary<string, bool> assetPacksActive = new Dictionary<string, bool>();

		public Dictionary<string, bool> texturePacksActive = new Dictionary<string, bool>();
	}

	private static ModsSettings _main;

	private static bool loaded;

	public static ModsSettings main
	{
		get
		{
			if (_main != null)
			{
				return _main;
			}
			_main = UnityEngine.Object.FindObjectOfType<ModsSettings>(includeInactive: true);
			if (_main == null || loaded)
			{
				return _main;
			}
			loaded = true;
			_main.Load();
			return _main;
		}
	}

	protected override string FileName => "ModsSettings";

	private void Awake()
	{
		_main = this;
		if (!loaded)
		{
			Load();
		}
	}

	public void SaveAll()
	{
		Save();
	}

	protected override void OnLoad()
	{
	}
}
