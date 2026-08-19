using System;
using SFS.Core;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using SFS.World;
using SFS.WorldBase;

namespace SFS.UI;

public class Quicksave_Saving : I_SavingBase
{
	private WorldReference paths;

	private Func<WorldSave> getQuicksave;

	private Action<WorldSave, I_MsgLogger> loadQuicksave;

	string I_SavingBase.Title => Loc.main.Quicksaves_Menu_Title;

	string I_SavingBase.LoadButtonText => Loc.main.Load;

	public Quicksave_Saving(WorldReference paths, Func<WorldSave> getQuicksave, Action<WorldSave, I_MsgLogger> loadQuicksave)
	{
		this.paths = paths;
		this.getQuicksave = getQuicksave;
		this.loadQuicksave = loadQuicksave;
	}

	ImportAvailability I_SavingBase.GetImportAvailability()
	{
		return ImportAvailability.NotAvailable;
	}

	bool I_SavingBase.CanSave(I_MsgLogger logger)
	{
		return true;
	}

	void I_SavingBase.Save(string name)
	{
		name = PathUtility.MakeUsable(name, Loc.main.Unnamed_Quicksave);
		if (paths.GetQuicksavesFileList().GetOrder().ConvertAll((string a) => a.ToLowerInvariant())
			.Contains(name.ToLowerInvariant()))
		{
			MenuGenerator.AskOverwrite(() => Loc.main.File_Already_Exists.InjectField(Loc.main.Quicksave, "filetype"), () => Loc.main.Overwrite_File.InjectField(Loc.main.Quicksave, "filetype"), () => Loc.main.New_File.InjectField(Loc.main.Quicksave, "filetype"), delegate
			{
				Save(name);
			}, delegate
			{
				Save(PathUtility.AutoNameExisting(name, paths.GetQuicksavesFileList()));
			});
		}
		else
		{
			Save(name);
		}
		void Save(string finalName)
		{
			paths.quicksavesPath.CreateFolder();
			WorldSave worldSave = getQuicksave();
			FolderPath path = paths.GetQuicksavePath(finalName);
			bool isCareer = Base.worldBase.IsCareer;
			SavingCache.SaveAsync(delegate
			{
				WorldSave.Save(path, saveRocketsAndBranches: true, worldSave, isCareer);
			});
			ScreenManager.main.CloseStack();
			MsgDrawer.main.Log(Loc.main.Saving_In_Progress);
		}
	}

	void I_SavingBase.Load(string name)
	{
		MsgCollector log = new MsgCollector();
		if (WorldSave.TryLoad(paths.GetQuicksavePath(name), loadRocketsAndBranches: true, log, out var worldSave))
		{
			loadQuicksave(worldSave, Menu.read);
			return;
		}
		ActionQueue.main.QueueMenu(Menu.read.Create(() => log.msg.ToString()));
	}

	void I_SavingBase.Import(string name)
	{
	}

	void I_SavingBase.Rename(string oldName, string newName)
	{
		if (!(oldName == newName))
		{
			newName = PathUtility.MakeUsable(newName, Loc.main.Unnamed_Quicksave);
			newName = PathUtility.AutoNameExisting(newName, paths.GetQuicksavesFileList());
			paths.GetQuicksavesFileList().Rename(oldName, newName);
			FolderPath quicksavePath = paths.GetQuicksavePath(oldName);
			FolderPath quicksavePath2 = paths.GetQuicksavePath(newName);
			if (quicksavePath.FolderExists())
			{
				quicksavePath.Move(quicksavePath2);
			}
		}
	}

	void I_SavingBase.Delete(string name)
	{
		paths.GetQuicksavePath(name).DeleteFolder();
		paths.GetQuicksavesFileList().Remove(name);
	}

	OrderedPathList I_SavingBase.GetOrderer()
	{
		return paths.GetQuicksavesFileList();
	}
}
