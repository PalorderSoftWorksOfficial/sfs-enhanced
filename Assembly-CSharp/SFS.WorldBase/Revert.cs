using SFS.IO;
using SFS.UI;
using SFS.World;

namespace SFS.WorldBase;

public static class Revert
{
	public static void AddToStack(WorldSave persistent, bool isCareer)
	{
		SavePointer(LoadPointer() + 1);
		WorldSave.Save(GetPath(0), saveRocketsAndBranches: true, persistent, isCareer);
		GetPath(15).DeleteFolder();
	}

	public static void DeleteAll()
	{
		Base.worldBase.paths.path.CloneAndExtend("Backup").DeleteFolder();
	}

	public static bool HasRevert_30_Sec()
	{
		return HasRevert(2);
	}

	public static bool HasRevert_3_Min()
	{
		return HasRevert(12);
	}

	public static bool TryGetRevert_30_Sec(out WorldSave save)
	{
		return TryGetRevert(2, out save);
	}

	public static bool TryGetRevert_3_Min(out WorldSave save)
	{
		return TryGetRevert(12, out save);
	}

	private static bool HasRevert(int cyclesBack)
	{
		if (GetPath(cyclesBack).FolderExists())
		{
			return GetPath(cyclesBack).ExtendToFile("Rockets.txt").FileExists();
		}
		return false;
	}

	private static bool TryGetRevert(int cyclesBack, out WorldSave save)
	{
		FolderPath path = GetPath(cyclesBack);
		if (!path.FolderExists())
		{
			save = null;
			return false;
		}
		bool result = WorldSave.TryLoad(path, loadRocketsAndBranches: true, MsgDrawer.main, out save);
		for (int num = cyclesBack - 1; num >= 0; num--)
		{
			GetPath(num).DeleteFolder();
		}
		SavePointer(LoadPointer() - cyclesBack);
		return result;
	}

	private static FolderPath GetPath(int cyclesBack)
	{
		Base.worldBase.paths.path.CloneAndExtend("Backup").CreateFolder();
		return Base.worldBase.paths.path.CloneAndExtend("Backup/Backup_" + (LoadPointer() - cyclesBack));
	}

	private static int LoadPointer()
	{
		FilePath pointerPath = GetPointerPath();
		if (!pointerPath.FileExists() || !int.TryParse(pointerPath.ReadText(), out var result))
		{
			return 0;
		}
		return result;
	}

	private static void SavePointer(int pointer)
	{
		GetPointerPath().WriteText(pointer.ToString());
	}

	private static FilePath GetPointerPath()
	{
		Base.worldBase.paths.path.CloneAndExtend("Backup").CreateFolder();
		return Base.worldBase.paths.path.ExtendToFile("Backup/RevertPointer.txt");
	}
}
