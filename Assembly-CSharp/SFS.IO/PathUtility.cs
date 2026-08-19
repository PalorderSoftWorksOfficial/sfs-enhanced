using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SFS.IO;

public static class PathUtility
{
	public static string MakeUsable(string name, string defaultName)
	{
		name = (IsValidFileName(name) ? name : defaultName);
		return FilePath.CleanupName(name);
	}

	public static string AutoNameExisting(string name, FolderPath path)
	{
		name = FilePath.CleanupName(name);
		List<string> list = (from file in path.GetFilesInFolder(recursively: false)
			select file.CleanFileName).ToList();
		list.AddRange(from folder in path.GetFoldersInFolder(recursively: false)
			select folder.FolderName);
		return UseUntakenName(name, list);
	}

	public static string AutoNameExisting(string name, OrderedPathList pathList)
	{
		name = FilePath.CleanupName(name);
		return UseUntakenName(name, pathList.GetOrder());
	}

	public static string UseUntakenName(string name, List<string> taken)
	{
		string text = name;
		int num = 1;
		while (true)
		{
			using List<string>.Enumerator enumerator = taken.GetEnumerator();
			do
			{
				if (!enumerator.MoveNext())
				{
					return name;
				}
			}
			while (!(enumerator.Current.ToLowerInvariant() == name.ToLowerInvariant()));
			name = text + " " + num;
			num++;
		}
	}

	private static bool IsValidFileName(string name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			return name.ToCharArray().ToList().Any((char c) => char.IsLetterOrDigit(c) && c != '.' && !Path.GetInvalidFileNameChars().Contains(c));
		}
		return false;
	}
}
