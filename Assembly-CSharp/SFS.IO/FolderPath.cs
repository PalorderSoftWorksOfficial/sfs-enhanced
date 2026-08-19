using System.Collections.Generic;
using System.IO;

namespace SFS.IO;

public class FolderPath : BasePath
{
	public string FolderName
	{
		get
		{
			return new DirectoryInfo(base.Path).Name;
		}
		private set
		{
			base.Path = GetParentPath() + "/" + value;
		}
	}

	public FolderPath Parent => new FolderPath(GetParentPath());

	public FolderPath(string initialLocation)
		: base(initialLocation)
	{
	}

	public FolderPath CloneAndExtend(string path)
	{
		return new FolderPath(base.Path + "/" + path.Replace("\\", "/"));
	}

	public FolderPath Extend(string path)
	{
		base.Path = base.Path + "/" + path.Replace("\\", "/");
		return this;
	}

	public FolderPath CreateFolder()
	{
		if (!FolderExists())
		{
			Directory.CreateDirectory(base.Path);
		}
		return this;
	}

	public void RenameFolder(string name)
	{
		Move(new FolderPath(GetParentPath()).Extend(name));
	}

	public void DeleteFolder()
	{
		if (FolderExists())
		{
			Directory.Delete(this, recursive: true);
		}
	}

	public FilePath ExtendToFile(string nameWithExtension)
	{
		return ExtendToFile(nameWithExtension, createNecessaryFolders: false);
	}

	public FilePath ExtendToFile(string nameWithExtension, bool createNecessaryFolders)
	{
		nameWithExtension = nameWithExtension.Replace("\\", "/");
		if (createNecessaryFolders)
		{
			for (int i = 0; i < nameWithExtension.Length; i++)
			{
				if (nameWithExtension[i] == '/')
				{
					FolderPath folderPath = new FolderPath(base.Path + "/" + nameWithExtension.Substring(0, i));
					if (!folderPath.FolderExists())
					{
						folderPath.CreateFolder();
					}
				}
			}
		}
		return new FilePath(base.Path + "/" + nameWithExtension);
	}

	public string GetRelativePath(string root)
	{
		return base.Path.Replace(System.IO.Path.GetFullPath(root).Replace("\\", "/"), "");
	}

	public IEnumerable<FilePath> GetFilesInFolder(bool recursively)
	{
		foreach (BasePath item in recursively ? EnumerateContentsRecursively() : EnumerateContents())
		{
			if (item is FilePath filePath)
			{
				yield return filePath;
			}
		}
	}

	public IEnumerable<FolderPath> GetFoldersInFolder(bool recursively)
	{
		foreach (BasePath item in recursively ? EnumerateContentsRecursively() : EnumerateContents())
		{
			if (item is FolderPath folderPath)
			{
				yield return folderPath;
			}
		}
	}

	public IEnumerable<BasePath> EnumerateContents()
	{
		foreach (string item in Directory.EnumerateDirectories(base.Path))
		{
			yield return new FolderPath(item);
		}
		foreach (string item2 in Directory.EnumerateFiles(base.Path))
		{
			yield return new FilePath(item2);
		}
	}

	public IEnumerable<BasePath> EnumerateContentsRecursively()
	{
		Stack<FolderPath> directories = new Stack<FolderPath>();
		foreach (FolderPath item in GetFoldersInFolder(recursively: false))
		{
			directories.Push(item);
		}
		foreach (FilePath item2 in GetFilesInFolder(recursively: false))
		{
			yield return item2;
		}
		while (directories.Count > 0)
		{
			FolderPath directory = directories.Pop();
			yield return directory;
			foreach (FolderPath item3 in directory.GetFoldersInFolder(recursively: false))
			{
				directories.Push(item3);
			}
			foreach (FilePath item4 in directory.GetFilesInFolder(recursively: false))
			{
				yield return item4;
			}
		}
	}

	public FolderPath Clone()
	{
		return new FolderPath(base.Path);
	}

	public bool FolderExists()
	{
		return Directory.Exists(this);
	}

	public void Move(FolderPath path)
	{
		if (!(base.Path == path.Path))
		{
			FolderPath folderPath = Clone();
			folderPath.FolderName += "_temporary_premove";
			Directory.Move(this, folderPath);
			path.DeleteFolder();
			Directory.Move(folderPath, path);
		}
	}
}
