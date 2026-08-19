using System;
using System.Collections.Generic;
using SFS.Core;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using SFS.UI;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.Builds;

public class Blueprint_Saving : I_SavingBase
{
	string I_SavingBase.Title => Loc.main.Blueprints_Menu_Title;

	string I_SavingBase.LoadButtonText => Loc.main.Load;

	ImportAvailability I_SavingBase.GetImportAvailability()
	{
		return ImportAvailability.ButtonEnabled;
	}

	bool I_SavingBase.CanSave(I_MsgLogger logger)
	{
		if (BuildManager.main.buildGrid.activeGrid.partsHolder.parts.Count > 0)
		{
			return true;
		}
		logger.Log(Loc.main.Cannot_Save_Empty_Build);
		return false;
	}

	void I_SavingBase.Save(string name)
	{
		name = PathUtility.MakeUsable(name, Loc.main.Unnamed_Blueprint);
		if (GetBlueprintsList().GetOrder().ConvertAll((string a) => a.ToLowerInvariant()).Contains(name.ToLowerInvariant()))
		{
			MenuGenerator.AskOverwrite(() => Loc.main.File_Already_Exists.InjectField(Loc.main.Blueprint, "filetype"), () => Loc.main.Overwrite_File.InjectField(Loc.main.Blueprint, "filetype"), () => Loc.main.New_File.InjectField(Loc.main.Blueprint, "filetype"), delegate
			{
				Save(name);
			}, delegate
			{
				Save(PathUtility.AutoNameExisting(name, GetBlueprintsList()));
			});
		}
		else
		{
			Save(name);
		}
		static void Save(string blueprintName)
		{
			Blueprint blueprint = BuildState.main.GetBlueprint();
			string version = Application.version;
			FolderPath path = GetPath(blueprintName);
			SavingCache.SaveAsync(delegate
			{
				Blueprint.Save(path, blueprint, version);
			});
			ScreenManager.main.CloseStack();
			MsgDrawer.main.Log(Loc.main.Saving_In_Progress);
		}
	}

	void I_SavingBase.Load(string name)
	{
		LoadBlueprint(name, delegate(Blueprint blueprint)
		{
			Undo.main.CreateNewStep("Load blueprint");
			BuildState.main.LoadBlueprint(blueprint, Menu.read, autoCenterParts: false, applyUndo: true);
		});
	}

	void I_SavingBase.Import(string name)
	{
		LoadBlueprint(name, delegate(Blueprint blueprint)
		{
			BuildManager.main.holdGrid.StartImport(blueprint, null);
		});
	}

	private static void LoadBlueprint(string name, Action<Blueprint> load)
	{
		MsgCollector log = new MsgCollector();
		if (Blueprint.TryLoad(GetPath(name), log, out var blueprint))
		{
			load(blueprint);
			return;
		}
		ActionQueue.main.QueueMenu(Menu.read.Create(() => log.msg.ToString()));
	}

	void I_SavingBase.Rename(string oldName, string newName)
	{
		if (!(oldName == newName))
		{
			newName = PathUtility.MakeUsable(newName, Loc.main.Unnamed_Blueprint);
			newName = PathUtility.AutoNameExisting(newName, GetBlueprintsList());
			GetBlueprintsList().Rename(oldName, newName);
			FolderPath path = GetPath(oldName);
			FolderPath path2 = GetPath(newName);
			if (path.FolderExists())
			{
				path.Move(path2);
			}
		}
	}

	void I_SavingBase.Delete(string name)
	{
		GetPath(name).DeleteFolder();
		GetBlueprintsList().Remove(name);
	}

	OrderedPathList I_SavingBase.GetOrderer()
	{
		return GetBlueprintsList();
	}

	private static FolderPath GetPath(string name)
	{
		return FileLocations.BlueprintsFolder.Extend(name);
	}

	private static OrderedPathList GetBlueprintsList()
	{
		if (!FileLocations.BlueprintsFolder.FolderExists())
		{
			FileLocations.BlueprintsFolder.CreateFolder();
		}
		List<BasePath> list = new List<BasePath>(FileLocations.BlueprintsFolder.GetFoldersInFolder(recursively: false));
		return new OrderedPathList(FileLocations.BlueprintsFolder, list.ToArray());
	}
}
