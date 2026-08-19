using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.Parts;
using SFS.Translations;
using SFS.UI;
using UnityEngine;

namespace ModLoader.UI;

public class ModsMenu : BasicMenu
{
	public RectTransform elementsHolder;

	public GameObject elementPrefab;

	private static List<(ModsListElement.ModData, ModsListElement)> elements = new List<(ModsListElement.ModData, ModsListElement)>();

	private bool drawnUI;

	public override void OnOpen()
	{
		if (!CustomAssetsLoader.finishedLoading)
		{
			Menu.read.ShowReport("Mods loading is not finished yet. Please wait.", delegate
			{
			});
			return;
		}
		base.OnOpen();
		if (!drawnUI)
		{
			DrawUI();
		}
		for (int num = 0; num < elements.Count; num++)
		{
			ModsListElement.ModData item = elements[num].Item1;
			ModsListElement item2 = elements[num].Item2;
			item2.iconHolder.gameObject.SetActive(value: true);
			if (item.icon != null)
			{
				item2.iconHolder.gameObject.SetActive(value: true);
				item2.iconImage.sprite = Sprite.Create(item.icon, new Rect(0f, 0f, item.icon.width, item.icon.height), new Vector2(0.5f, 0.5f));
			}
			else if (item.loadingTexture != null && item.loadingTexture.IsCompleted && item.loadingTexture.Result != null)
			{
				item2.iconHolder.gameObject.SetActive(value: true);
				Texture2D result = item.loadingTexture.Result;
				item2.iconImage.sprite = Sprite.Create(result, new Rect(0f, 0f, result.width, result.height), new Vector2(0.5f, 0.5f));
			}
			else
			{
				item2.iconHolder.gameObject.SetActive(value: false);
			}
		}
	}

	public static void AddElement(ModsListElement.ModData data)
	{
		switch (data.type)
		{
		case ModsListElement.ModType.Mod:
			if (!ModsSettings.main.settings.modsActive.ContainsKey(data.saveName))
			{
				ModsSettings.main.settings.modsActive.Add(data.saveName, value: true);
			}
			break;
		case ModsListElement.ModType.AssetsPack:
			if (!ModsSettings.main.settings.assetPacksActive.ContainsKey(data.saveName))
			{
				ModsSettings.main.settings.assetPacksActive.Add(data.saveName, value: true);
			}
			break;
		case ModsListElement.ModType.TexturesPack:
			if (!ModsSettings.main.settings.texturePacksActive.ContainsKey(data.saveName))
			{
				ModsSettings.main.settings.texturePacksActive.Add(data.saveName, value: true);
			}
			break;
		}
		ModsSettings.main.SaveAll();
		elements.Add((data, null));
	}

	private void DrawUI()
	{
		for (int i = 0; i < elements.Count; i++)
		{
			ModsListElement.ModData item = elements[i].Item1;
			GameObject obj = UnityEngine.Object.Instantiate(elementPrefab, elementsHolder);
			obj.gameObject.SetActive(value: true);
			ModsListElement component = obj.GetComponent<ModsListElement>();
			component.Init(item);
			elements[i] = (item, component);
		}
		drawnUI = true;
	}

	public void OpenModsFolder()
	{
		Application.OpenURL(new Uri(Loader.ModsFolder).AbsoluteUri);
	}

	public void OpenModDownloads()
	{
		Application.OpenURL("https://jmnet.one/sfs/forum/index.php?forums/authorised-game-mods.101/");
	}

	public void CloseWindow()
	{
		ModsSettings.main.SaveAll();
		if (elements.Any(((ModsListElement.ModData, ModsListElement) x) => x.Item2.IsChanged))
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Changes_Warning, () => Loc.main.Relaunch, ApplicationUtility.Relaunch, null, Close);
		}
		else
		{
			Close();
		}
	}
}
