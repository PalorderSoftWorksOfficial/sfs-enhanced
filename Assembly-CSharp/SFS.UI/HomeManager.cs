using System;
using System.Collections.Generic;
using SFS.Analytics;
using SFS.Audio;
using SFS.Input;
using SFS.Translations;
using UnityEngine;

namespace SFS.UI;

public class HomeManager : BasicMenu
{
	public static HomeManager main;

	[Space]
	[Space]
	public MusicPlaylistPlayer menuMusic;

	public TextAdapter version;

	public Material starsMaterial;

	public DevelopmentMenu developmentMenu;

	[Space]
	public GameObject steamBanner;

	public Button settingsButton;

	public GameObject fullVersionButton;

	public BasicMenu fullVersionSalePage;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		fullVersionButton.SetActive(DevSettings.ShowFullVersionButton);
		RemoteSettings.ForceUpdate();
		menuMusic.StartPlaying(5f);
		starsMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 1f));
		string versionText = GetVersion();
		version.Text = "v" + versionText;
		if (!string.IsNullOrEmpty(Base.sceneLoader.sceneSettings.openShop))
		{
			if (Base.sceneLoader.sceneSettings.openShop == "Full Version")
			{
				fullVersionSalePage.Open();
			}
			return;
		}
		LanguageSettings.main.Initialize(delegate
		{
			ShowUpgradeVersionMenu(versionText, delegate
			{
				ShowIsNewPlayerMenu(delegate
				{
				});
			});
		});
	}

	private static void ShowIsNewPlayerMenu(Action callback)
	{
	}

	private static void ShowUpgradeVersionMenu(string versionText, Action callback)
	{
		callback();
	}

	private static string GetVersion()
	{
		return Application.version;
	}

	public void OpenFullVersion()
	{
		fullVersionSalePage.Open();
	}

	public void OpenCommunity()
	{
		List<MenuElement> list = new List<MenuElement>
		{
			new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Youtube, delegate
			{
				Application.OpenURL("https://www.youtube.com/channel/UCOpgvpnGyZw4IRT_kuebiWA");
			}, CloseMode.Current).MinSize(300f, 60f),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Discord, delegate
			{
				Application.OpenURL("https://discordapp.com/invite/hwfWm2d");
			}, CloseMode.Current).MinSize(300f, 60f),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Reddit, delegate
			{
				Application.OpenURL("https://www.reddit.com/r/SpaceflightSimulator/");
			}, CloseMode.Current).MinSize(300f, 60f),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Community_Forums, delegate
			{
				Application.OpenURL("https://jmnet.one/sfs/forum/index.php");
			}, CloseMode.Current).MinSize(300f, 60f)
		};
		if (LanguageSettings.main.settings.name == "Russian")
		{
			list.Add(ButtonBuilder.CreateButton(carrier, () => "ВКонтакте", delegate
			{
				Application.OpenURL("https://vk.com/public194508161");
			}, CloseMode.Current).MinSize(300f, 60f));
		}
		ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Close, CloseMode.Current, delegate
		{
		}, delegate
		{
		}, list.ToArray()));
	}

	public void OpenSettings()
	{
		Menu.settings.Open();
	}

	public void OpenCredits()
	{
		Menu.read.Open(() => Loc.main.Credits_Text, CloseMode.Current, background: false);
	}

	public void OpenPC()
	{
		Application.OpenURL("https://spaceflight-simulator-steam.azurewebsites.net/");
	}

	public static void OpenTutorials_Static()
	{
		string link_Orbit = FbRemoteSettings.GetString("Video_Tutorial_Orbit");
		string link_Moon = FbRemoteSettings.GetString("Video_Tutorial_Moon");
		string link_Dock = FbRemoteSettings.GetString("Video_Tutorial_Dock");
		List<MenuElement> list = new List<MenuElement>
		{
			new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Orbit, delegate
			{
				Application.OpenURL(link_Orbit);
			}, CloseMode.Current).MinSize(300f, 60f),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Moon, delegate
			{
				Application.OpenURL(link_Moon);
			}, CloseMode.Current).MinSize(300f, 60f),
			ButtonBuilder.CreateButton(carrier, () => Loc.main.Video_Dock, delegate
			{
				Application.OpenURL(link_Dock);
			}, CloseMode.Current).MinSize(300f, 60f)
		};
		ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Close, CloseMode.Current, delegate
		{
		}, delegate
		{
		}, list.ToArray()));
	}

	public override void Close()
	{
		MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Close_Game, () => Loc.main.Close, Application.Quit);
	}
}
