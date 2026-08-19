using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class WorldsMenu : MonoBehaviour
{
	public RectTransform worldElementHolder;

	public ScrollElement scrollElement;

	public ReorderingGroup reorderingGroup;

	public RadioGroup worldsGroup;

	public WorldElement worldElementPrefab;

	public RectMask2D mask;

	public Button[] worldButtons;

	private List<WorldElement> elements = new List<WorldElement>();

	private OrderedPathList orderer;

	private WorldElement selected;

	private void Start()
	{
		worldElementPrefab.gameObject.SetActive(value: false);
		UpdateOrder();
		Loc.OnChange += new Action(LoadUI);
		RadioGroup radioGroup = worldsGroup;
		radioGroup.onSelectionChanged = (Action<RadioButton>)Delegate.Combine(radioGroup.onSelectionChanged, new Action<RadioButton>(UpdateWorldActions));
		ReorderingGroup obj = reorderingGroup;
		obj.onReorder = (Action<ReorderingModule>)Delegate.Combine(obj.onReorder, new Action<ReorderingModule>(OnReorder));
	}

	private void OnDestroy()
	{
		Loc.OnChange -= new Action(LoadUI);
	}

	private void OnEnable()
	{
		LoadUI((selected != null) ? selected.Name : null);
	}

	private void UpdateWorldActions(RadioButton selectedButton)
	{
		selected = elements.SingleOrDefault((WorldElement element) => element.radioButton == selectedButton);
		worldButtons.ForEach(delegate(Button button)
		{
			button.SetEnabled(selected != null);
		});
	}

	public void OpenHub()
	{
		WorldBaseManager.EnterWorld(selected.Name, Base.sceneLoader.LoadHubScene);
	}

	public void OpenWorld()
	{
		if (selected.World.worldPersistentPath.FolderExists())
		{
			WorldBaseManager.EnterWorld(selected.Name, delegate
			{
				Base.sceneLoader.LoadWorldScene();
			});
		}
	}

	public void DeleteWorld()
	{
		MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.World_Delete, () => Loc.main.Delete, delegate
		{
			selected.World.path.DeleteFolder();
			orderer.Remove(selected.Name);
			LoadUI();
		});
	}

	public void RenameWorld()
	{
		Menu.textInput.Open(Loc.main.Cancel, Loc.main.Rename, delegate(string[] newName)
		{
			string text = newName[0];
			text = PathUtility.MakeUsable(text, Loc.main.Default_World_Name);
			if (!(text == selected.Name))
			{
				text = PathUtility.AutoNameExisting(text, FileLocations.WorldsFolder);
				orderer.Rename(selected.Name, text);
				selected.World.path.RenameFolder(text);
				LoadUI(text);
			}
		}, CloseMode.Current, TextInputMenu.Element(Loc.main.Create_World_Title, selected.Name));
	}

	private void OnReorder(ReorderingModule reorderedModule)
	{
		WorldElement worldElement = elements.First((WorldElement x) => x.reorderingModule == reorderedModule);
		orderer.Move(worldElement.Name, worldElement.transform.GetSiblingIndex() - 1);
	}

	private void LoadUI()
	{
		LoadUI(null);
	}

	private void LoadUI(string autoSelectTarget)
	{
		elements.ForEach(delegate(WorldElement x)
		{
			UnityEngine.Object.Destroy(x.gameObject);
		});
		elements.Clear();
		UpdateOrder();
		WorldElement worldElement = null;
		foreach (string item in orderer.GetOrder())
		{
			WorldElement worldElement2 = UnityEngine.Object.Instantiate(worldElementPrefab, worldElementHolder);
			worldElement2.gameObject.SetActive(value: true);
			worldElement2.Setup(new WorldReference(FileLocations.WorldsFolder.Extend(item).FolderName));
			worldElement2.radioButton.group = worldsGroup;
			elements.Add(worldElement2);
			reorderingGroup.Add(worldElement2.reorderingModule);
			scrollElement.RegisterScrolling(worldElement2.button, () => !reorderingGroup.holding);
			if (item == autoSelectTarget)
			{
				worldElement2.radioButton.Select();
				worldElement = worldElement2;
			}
		}
		UpdateWorldActions((worldElement != null) ? worldElement.radioButton : null);
	}

	private void UpdateOrder()
	{
		orderer = GetOrdered();
	}

	public static OrderedPathList GetOrdered()
	{
		return new OrderedPathList(FileLocations.WorldsFolder, FileLocations.WorldsFolder.FolderExists() ? FileLocations.WorldsFolder.GetFoldersInFolder(recursively: false).Select((Func<FolderPath, BasePath>)((FolderPath f) => f)).ToArray() : new BasePath[0]);
	}

	public void OnWorldCreated(string worldName)
	{
		UpdateOrder();
		LoadUI(worldName);
	}
}
