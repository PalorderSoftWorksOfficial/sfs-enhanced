using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Input;
using SFS.Sharing;
using SFS.Translations;
using UnityEngine;

namespace SFS.UI;

public class DownloadMenu : MonoBehaviour
{
	private SharingRequester requester = new SharingRequester();

	public void OpenSharingMenu_PC()
	{
		List<MenuElement> list = new List<MenuElement> { new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize) };
		list.Add(ButtonBuilder.CreateButton(carrier, () => Loc.main.Upload_Blueprint_PC, UploadRocket, CloseMode.Current).MinSize(300f, 60f).CustomizeButton(delegate(Button a)
		{
			a.SetEnabled(BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count > 0);
		}));
		list.Add(ButtonBuilder.CreateButton(carrier, () => Loc.main.Download_Blueprint_PC, OpenDownloadMenu, CloseMode.Current).MinSize(300f, 60f));
		list.Add(ButtonBuilder.CreateButton(carrier, () => Loc.main.Cancel, delegate
		{
		}, CloseMode.Current).MinSize(300f, 60f));
		ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.Cancel, CloseMode.Current, delegate
		{
		}, delegate
		{
		}, list.ToArray()));
	}

	private void OpenDownloadMenu()
	{
		Menu.textInput.Open(Loc.main.Cancel, Loc.main.Download_Confirm, delegate(string[] strings)
		{
			string text = strings[0];
			if (!text.StartsWith("https://sharing.spaceflightsimulator.app") && !text.StartsWith("sharing.spaceflightsimulator.app"))
			{
				MsgDrawer.main.Log(Loc.main.URL_Invalid);
			}
			else
			{
				string[] array = text.Split('/', StringSplitOptions.None);
				string rocketID = array[array.Length - 1];
				DownloadRocket(rocketID);
			}
		}, CloseMode.Current, CloseMode.Stack, TextInputMenu.Element(Loc.main.URL_Field_TextBox, string.Empty));
	}

	public void UploadRocket()
	{
		Menu.loading.Open(Loc.main.Uploading_Message);
		requester.Initialize(delegate(InitializationResult result)
		{
			if (result != InitializationResult.Success)
			{
				Menu.loading.Close();
				MsgDrawer.main.Log(Loc.main.Sharing_Connect_Fail);
			}
			else
			{
				Blueprint blueprint = BuildState.main.GetBlueprint();
				if (blueprint.parts.Length == 0)
				{
					Menu.loading.Close();
					MsgDrawer.main.Log(Loc.main.Empty_Upload);
				}
				else
				{
					ImageTools.RenderTextureTo2D(PartIconCreator.main.CreatePartIcon_Sharing(blueprint, 1000, 2000), 1000, 2000);
					requester.UploadRocketLinked(blueprint, "", delegate(bool success, string url)
					{
						if (!success)
						{
							Menu.loading.Close();
							MsgDrawer.main.Log(Loc.main.Upload_Fail);
						}
						else
						{
							MsgDrawer.main.Log(Loc.main.Copied_URL_To_Clipboard);
							GUIUtility.systemCopyBuffer = url;
							Menu.loading.Close();
						}
					});
				}
			}
		});
	}

	public void DownloadRocket(string rocketID)
	{
		Menu.loading.Open(Loc.main.Downloading_Message);
		MsgDrawer.main.Log("");
		requester.Initialize(delegate(InitializationResult result)
		{
			MsgDrawer.main.Log("");
			if (result != InitializationResult.Success)
			{
				Menu.loading.Close();
				MsgDrawer.main.Log(Loc.main.Sharing_Connect_Fail);
			}
			else
			{
				MsgDrawer.main.Log("");
				requester.GetRocket(rocketID, delegate(bool success, Blueprint blueprint)
				{
					MsgDrawer.main.Log("");
					if (!success)
					{
						Menu.loading.Close();
						MsgDrawer.main.Log(Loc.main.Download_Fail);
						return;
					}
					Menu.loading.Close();
					try
					{
						Undo.main.CreateNewStep("download");
						BuildState.main.LoadBlueprint(blueprint, Menu.read, autoCenterParts: true, applyUndo: true);
					}
					catch (Exception value)
					{
						Console.WriteLine(value);
						MsgDrawer.main.Log(Loc.main.Download_Fail);
					}
				});
			}
		});
	}
}
