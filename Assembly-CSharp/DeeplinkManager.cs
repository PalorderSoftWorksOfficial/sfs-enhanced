using System;
using System.Collections;
using System.Collections.Generic;
using SFS;
using SFS.Builds;
using SFS.Career;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

public class DeeplinkManager : MonoBehaviour
{
	public static DeeplinkManager main;

	public string deeplinkUrl_Loaded = "[none]";

	public string deeplinkUrl_Moved = "[none]";

	private void Awake()
	{
		main = this;
		Application.deepLinkActivated += OnDeepLinkActivated;
		if (!string.IsNullOrEmpty(Application.absoluteURL))
		{
			OnDeepLinkActivated(Application.absoluteURL);
		}
		else
		{
			deeplinkUrl_Loaded = "[none]";
		}
	}

	private void OnDeepLinkActivated(string url)
	{
		string[] array = url.Split("/"[0], StringSplitOptions.None);
		string text = array[((ICollection)array).Count - 1];
		deeplinkUrl_Loaded = text;
	}

	private void Update()
	{
		HandleLoaded();
		HandleMoved();
	}

	private void HandleLoaded()
	{
		string text = deeplinkUrl_Loaded;
		if (text == "[none]" || text == "" || text == null)
		{
			return;
		}
		if (HomeManager.main != null)
		{
			string deeplinkUrlCopy = deeplinkUrl_Loaded;
			deeplinkUrl_Loaded = "[none]";
			List<MenuElement> list = new List<MenuElement> { new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize) };
			List<string> order = WorldsMenu.GetOrdered().GetOrder();
			if (order.Count == 0)
			{
				Menu.read.Open(() => "You must create a world to download blueprints");
				return;
			}
			if (order.Count == 1)
			{
				WorldBaseManager.EnterWorld(order[0], delegate
				{
					Base.sceneLoader.LoadBuildScene(askBuildNew: false);
				});
				return;
			}
			list.Add(TextBuilder.CreateText(() => Loc.main.Sharing_Enter_Prompt));
			list.Add(ElementGenerator.VerticalSpace(50));
			foreach (string item in order)
			{
				string worldNameCopy = item;
				list.Add(ButtonBuilder.CreateButton(carrier, () => worldNameCopy, delegate
				{
					ScreenManager.main.CloseCurrent();
					deeplinkUrl_Moved = deeplinkUrlCopy;
					WorldBaseManager.EnterWorld(worldNameCopy, delegate
					{
						Base.sceneLoader.LoadBuildScene(askBuildNew: false);
					});
				}, CloseMode.None).MinSize(300f, 60f));
			}
			ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Cancel, CloseMode.None, delegate
			{
			}, delegate
			{
			}, list.ToArray()));
		}
		else if (GameManager.main != null)
		{
			deeplinkUrl_Moved = deeplinkUrl_Loaded;
			deeplinkUrl_Loaded = "[none]";
			GameManager.main.ExitToBuild();
		}
		else
		{
			if (HubManager.main != null)
			{
				deeplinkUrl_Moved = deeplinkUrl_Loaded;
				deeplinkUrl_Loaded = "[none]";
				HubManager.main.EnterBuild();
			}
			if (BuildManager.main != null)
			{
				deeplinkUrl_Moved = deeplinkUrl_Loaded;
				deeplinkUrl_Loaded = "[none]";
			}
		}
	}

	private void HandleMoved()
	{
		string text = deeplinkUrl_Moved;
		if (text == "[none]" || text == "" || text == null)
		{
			return;
		}
		MsgDrawer.main.Log("");
		if (!(BuildManager.main != null))
		{
			return;
		}
		string deeplinkUrlCopy = deeplinkUrl_Moved;
		deeplinkUrl_Moved = "[none]";
		if (BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count > 0)
		{
			MsgDrawer.main.Log("");
			MenuGenerator.OpenMenu(CancelButton.None, CloseMode.Stack, new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize), ButtonBuilder.CreateIconButton(carrier, ResourcesLoader.main.buttonIcons.load, () => Loc.main.Confirm_Download_Button, Load, CloseMode.Stack), ButtonBuilder.CreateButton(carrier, () => Loc.main.Cancel, delegate
			{
			}, CloseMode.Stack));
		}
		else
		{
			MsgDrawer.main.Log("");
			Load();
		}
		void Load()
		{
			MsgDrawer.main.Log("");
			BuildManager.main.downloadMenu.DownloadRocket(deeplinkUrlCopy);
		}
	}
}
