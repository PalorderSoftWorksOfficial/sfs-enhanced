using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFS.Translations;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModLoader.UI;

public class ModsListElement : MonoBehaviour
{
	public struct ModData
	{
		public ModType type;

		public string name;

		public string version;

		public string description;

		public string author;

		public string saveName;

		public Texture2D icon;

		public Task<Texture2D> loadingTexture;
	}

	public enum ModType
	{
		Mod,
		AssetsPack,
		TexturesPack
	}

	public CanvasGroup canvasGroup;

	public ToggleButton activeToggle;

	public TextAdapter versionText;

	public TextAdapter descriptionText;

	public SFS.UI.Button switchButton;

	public RectTransform iconHolder;

	public Image iconImage;

	public TextAdapter nameText;

	public TextAdapter typeText;

	public TextAdapter authorText;

	private Func<bool> active;

	private Action toggle;

	private bool activeOld;

	public bool IsChanged => active() != activeOld;

	public void Init(ModData data)
	{
		active = data.type switch
		{
			ModType.Mod => () => ModsSettings.main.settings.modsActive[data.saveName], 
			ModType.AssetsPack => () => ModsSettings.main.settings.assetPacksActive[data.saveName], 
			ModType.TexturesPack => () => ModsSettings.main.settings.texturePacksActive[data.saveName], 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		activeOld = active();
		toggle = data.type switch
		{
			ModType.Mod => delegate
			{
				Dictionary<string, bool> modsActive = ModsSettings.main.settings.modsActive;
				string saveName = data.saveName;
				modsActive[saveName] = !modsActive[saveName];
				ModsSettings.main.SaveAll();
				CheckState();
			}, 
			ModType.AssetsPack => delegate
			{
				Dictionary<string, bool> assetPacksActive = ModsSettings.main.settings.assetPacksActive;
				string saveName = data.saveName;
				assetPacksActive[saveName] = !assetPacksActive[saveName];
				ModsSettings.main.SaveAll();
				CheckState();
			}, 
			ModType.TexturesPack => delegate
			{
				Dictionary<string, bool> texturePacksActive = ModsSettings.main.settings.texturePacksActive;
				string saveName = data.saveName;
				texturePacksActive[saveName] = !texturePacksActive[saveName];
				ModsSettings.main.SaveAll();
				CheckState();
			}, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		activeToggle.Bind(toggle, active);
		descriptionText.Text = data.description;
		versionText.Text = data.version;
		nameText.Text = data.name;
		TextAdapter textAdapter = typeText;
		textAdapter.Text = data.type switch
		{
			ModType.Mod => Loc.main.Mod_Name, 
			ModType.AssetsPack => Loc.main.AssetPack_Name, 
			ModType.TexturesPack => Loc.main.TexturePack_Name, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		authorText.Text = data.author;
		CheckState();
	}

	private void CheckState()
	{
		canvasGroup.alpha = (active() ? 1f : 0.8f);
	}
}
