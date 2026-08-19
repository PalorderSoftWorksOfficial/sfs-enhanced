using System;
using System.Collections.Generic;
using System.IO;
using SFS.Parsers.Json;

namespace SFS.IO;

public class OrderedPathList
{
	private const string OrderFileName = "display_order.ordr";

	private FolderPath root;

	private List<string> fileNames;

	public OrderedPathList(FolderPath root, BasePath[] paths)
	{
		this.root = root;
		fileNames = new List<string>();
		if (JsonWrapper.TryLoadJson<List<string>>(root.ExtendToFile("display_order.ordr"), out var data))
		{
			string[] array = data.ToArray();
			foreach (string text in array)
			{
				if (root.ExtendToFile(text).FileExists() || root.CloneAndExtend(text).FolderExists())
				{
					fileNames.Add(text);
				}
			}
		}
		Array.Sort(paths, delegate(BasePath a, BasePath b)
		{
			return GetCreationTime(a).CompareTo(GetCreationTime(b));
			static DateTime GetCreationTime(BasePath basePath2)
			{
				if (basePath2 is FilePath filePath2)
				{
					return new FileInfo(filePath2).CreationTime;
				}
				if (basePath2 is FolderPath folderPath2)
				{
					return new DirectoryInfo(folderPath2).CreationTime;
				}
				return default(DateTime);
			}
		});
		foreach (BasePath basePath in paths)
		{
			string item;
			if (basePath is FilePath filePath)
			{
				item = filePath.FileName;
			}
			else
			{
				if (!(basePath is FolderPath folderPath))
				{
					continue;
				}
				item = folderPath.FolderName;
			}
			if (!fileNames.Contains(item))
			{
				fileNames.Add(item);
			}
		}
	}

	public List<string> GetOrder()
	{
		List<string> list = new List<string>();
		foreach (string fileName in fileNames)
		{
			FilePath filePath = root.ExtendToFile(fileName);
			FolderPath folderPath = root.CloneAndExtend(fileName);
			if (filePath.FileExists())
			{
				list.Add(filePath.CleanFileName);
			}
			else if (folderPath.FolderExists())
			{
				list.Add(folderPath.FolderName);
			}
		}
		return list;
	}

	public void Rename(string oldName, string newName)
	{
		if (fileNames.Contains(oldName))
		{
			int index = fileNames.IndexOf(oldName);
			fileNames[index] = newName;
			SaveOrder();
		}
	}

	public void Move(string name, int newIndex)
	{
		if (fileNames.Contains(name))
		{
			newIndex = Math.Max(0, newIndex);
			fileNames.Remove(name);
			fileNames.Insert(newIndex, name);
			SaveOrder();
		}
	}

	public void Remove(string name)
	{
		fileNames.RemoveAll((string fileName) => fileName == name);
		SaveOrder();
	}

	private void SaveOrder()
	{
		JsonWrapper.SaveAsJson(root.ExtendToFile("display_order.ordr"), fileNames, pretty: false);
	}
}
