using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ModLoader.UI;
using SFS.IO;
using UnityEngine;

namespace ModLoader;

public class Loader : MonoBehaviour
{
	public static Loader main;

	private List<Mod> mods;

	private List<Mod> loadedMods = new List<Mod>();

	private static readonly Regex Version_Regex = new Regex("\\b([0-9]+\\.)+[0-9]+\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public static FolderPath ModsFolder => FileLocations.BaseFolder.Extend("/../Mods");

	public Mod[] GetAllMods()
	{
		return mods.ToArray();
	}

	public Mod[] GetLoadedMods()
	{
		return loadedMods.ToArray();
	}

	public void Initialize_EarlyLoad()
	{
		ModsFolder.CreateFolder();
		MoveIndividualDLLs();
		LoadModList();
		foreach (Mod mod in mods)
		{
			try
			{
				LoadMod(mod);
			}
			catch (Exception exception)
			{
				Debug.Log("Failed to load " + mod.DisplayName);
				Debug.LogException(exception);
			}
		}
	}

	public void Initialize_Load()
	{
		foreach (Mod loadedMod in loadedMods)
		{
			try
			{
				loadedMod.LoadKeybindings?.Invoke();
				loadedMod.Load();
				Debug.Log("Loaded " + loadedMod.DisplayName);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}

	private void MoveIndividualDLLs()
	{
		try
		{
			foreach (FilePath item in ModsFolder.GetFilesInFolder(recursively: false))
			{
				if (item.Extension == "dll")
				{
					item.Move(ModsFolder.Extend(item.CleanFileName).CreateFolder().ExtendToFile(item.FileName));
				}
			}
		}
		catch (Exception exception)
		{
			Debug.Log("Failed to move Individual DLLs due to:");
			Debug.LogException(exception);
		}
	}

	private void LoadModList()
	{
		mods = new List<Mod>();
		List<FilePath> list = new List<FilePath>();
		foreach (FolderPath item in ModsFolder.GetFoldersInFolder(recursively: false))
		{
			if (!(item.FolderName == "Custom Assets"))
			{
				FilePath filePath = item.ExtendToFile(item.FolderName + ".dll");
				try
				{
					LoadAssembly(filePath);
				}
				catch (ReflectionTypeLoadException)
				{
					list.Add(filePath);
				}
				catch (TypeLoadException)
				{
					list.Add(filePath);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
		foreach (FilePath item2 in list)
		{
			try
			{
				LoadAssembly(item2);
			}
			catch (Exception exception2)
			{
				Debug.LogException(exception2);
			}
		}
		void LoadAssembly(FilePath path)
		{
			Type type = Assembly.Load(path.ReadBytes()).GetTypes().FirstOrDefault((Type x) => typeof(Mod).IsAssignableFrom(x));
			if (type == null)
			{
				throw new Exception("Mod has no entry point: " + path.CleanFileName);
			}
			Mod mod = Activator.CreateInstance(type) as Mod;
			if (mods.Any((Mod x) => x.ModNameID == mod.ModNameID))
			{
				throw new Exception("Mod with same name already loaded: " + path.CleanFileName);
			}
			mod.ModFolder = path.GetParent();
			mods.Add(mod);
		}
	}

	private void LoadMod(Mod mod)
	{
		if (loadedMods.Any((Mod x) => x.ModNameID == mod.ModNameID))
		{
			return;
		}
		if (!VerifyVersion(mod.MinimumGameVersionNecessary, Application.version))
		{
			Debug.Log("This game version is too low for " + mod.DisplayName);
		}
		else if (LoadDependencies(mod))
		{
			ModsMenu.AddElement(new ModsListElement.ModData
			{
				author = mod.Author,
				description = mod.Description,
				icon = null,
				name = mod.DisplayName,
				type = ModsListElement.ModType.Mod,
				version = mod.ModVersion,
				saveName = mod.ModNameID,
				loadingTexture = TextureUtility.GetRemoteTexture(mod.IconLink)
			});
			if (CollectionExtensions.GetValueOrDefault<string, bool>((IReadOnlyDictionary<string, bool>)ModsSettings.main.settings.modsActive, mod.ModNameID, true))
			{
				mod.Early_Load();
				loadedMods.Add(mod);
			}
		}
	}

	private static bool VerifyVersion(string targetVersion, string currentVersion)
	{
		if (!Version_Regex.IsMatch(targetVersion) || !Version_Regex.IsMatch(currentVersion))
		{
			return false;
		}
		int[] array = (from x in targetVersion.Split(".", StringSplitOptions.None)
			select int.Parse(x)).ToArray();
		int[] array2 = (from x in currentVersion.Split(".", StringSplitOptions.None)
			select int.Parse(x)).ToArray();
		foreach (int item in Enumerable.Range(0, Math.Min(array.Length, array2.Length) - 1))
		{
			if (array2[item] > array[item])
			{
				return true;
			}
			if (array2[item] < array[item])
			{
				return false;
			}
		}
		return true;
	}

	private bool LoadDependencies(Mod mod)
	{
		if (mod.Dependencies == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, string> item in mod.Dependencies)
		{
			if (mods.Any((Mod x) => x.ModNameID == item.Key))
			{
				Mod mod2 = mods.Find((Mod x) => x.ModNameID == item.Key);
				if (VerifyVersion(item.Value, mod2.ModVersion))
				{
					if (!CollectionExtensions.GetValueOrDefault<string, bool>((IReadOnlyDictionary<string, bool>)ModsSettings.main.settings.modsActive, mod2.ModNameID, true))
					{
						Debug.Log("Dependency mod of " + mod.DisplayName + " is disabled: " + mod2.DisplayName);
						return false;
					}
					LoadMod(mod2);
					continue;
				}
				Debug.Log(mod2.DisplayName + " version is too low for " + mod.DisplayName);
				return false;
			}
			Debug.Log(mod.DisplayName + " need dependency: " + item.Key);
			return false;
		}
		return true;
	}
}
