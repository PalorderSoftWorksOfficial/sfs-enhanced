using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Builds;

public class SkinMenu : ExpandMenu
{
	public RectTransform buySkinsButton;

	public SkinMenuButton texturePrefab;

	public int channel;

	private Pool<SkinMenuButton> skinButtons;

	public RectTransform fullVersionButton;

	private void Awake()
	{
		skinButtons = new Pool<SkinMenuButton>(delegate
		{
			SkinMenuButton button = UnityEngine.Object.Instantiate(texturePrefab, base.transform);
			if (skinButtons.Items.Count == 0)
			{
				buySkinsButton.SetSiblingIndex(1);
			}
			button.button.onClick += (Action)delegate
			{
				if (expanded.Value)
				{
					ApplySkin(button.texture);
				}
				ToggleExpanded();
			};
			return button;
		}, delegate(SkinMenuButton button)
		{
			button.gameObject.SetActive(value: false);
		});
	}

	private void Start()
	{
		expanded.OnChange += new Action(DrawSkinButtons);
	}

	private void ApplySkin(PartTexture texture)
	{
		Part[] array = BuildManager.main.selector.selected.ToArray();
		SetSkin(array, symmetry: false);
		if (BuildManager.main.symmetryMode)
		{
			SetSkin(BuildManager.main.buildGrid.GetMirrorParts(array), symmetry: true);
		}
		void SetSkin(Part[] a, bool symmetry)
		{
			Undo.main.RecordStatChangeStep(a, delegate
			{
				foreach (SkinModule module in Part_Utility.GetModules<SkinModule>(a))
				{
					if (module.GetTextureOptions(channel).Contains(texture))
					{
						module.SetTexture(channel, texture);
					}
				}
			}, !symmetry);
		}
	}

	public void DrawSkinButtons()
	{
		List<PartTexture> textureOptions = GetTextureOptions(Part_Utility.GetModules<SkinModule>(BuildManager.main.selector.selected.ToArray()).ToArray(), channel);
		if (textureOptions.Count < 2)
		{
			if (this != null)
			{
				base.gameObject.SetActive(value: false);
			}
			return;
		}
		base.gameObject.SetActive(value: true);
		buySkinsButton.gameObject.SetActive(expanded.Value && !DevSettings.FullVersion);
		int num = ((!expanded.Value || !DevSettings.FullVersion) ? 1 : textureOptions.Count);
		skinButtons.Reset();
		for (int i = 0; i < num; i++)
		{
			SkinMenuButton item = skinButtons.GetItem();
			item.gameObject.SetActive(value: true);
			item.SetTexture(textureOptions[i]);
		}
	}

	private static List<PartTexture> GetTextureOptions(SkinModule[] skinModules, int channel)
	{
		if (skinModules.Length == 0)
		{
			return new List<PartTexture>();
		}
		List<PartTexture> list = new List<PartTexture> { skinModules.Last().GetTexture(channel) };
		for (int i = 0; i < skinModules.Length; i++)
		{
			foreach (PartTexture textureOption in skinModules[i].GetTextureOptions(channel))
			{
				if (!list.Contains(textureOption))
				{
					list.Add(textureOption);
				}
			}
		}
		return list;
	}
}
