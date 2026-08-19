using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class LoadMenu : Screen_Menu
{
	public I_SavingBase implementation;

	public RectTransform holder;

	public RectTransform loadFilesHolder;

	public ScrollElement scrollElement;

	public ReorderingGroup reorderingGroup;

	public RadioGroup savesGroup;

	public LoadMenuElement elementPrefab;

	public TextAdapter title;

	public TextAdapter loadButtonText;

	public Button deleteButton;

	public Button loadButton;

	public Button renameButton;

	public Button importButton;

	private List<LoadMenuElement> elements = new List<LoadMenuElement>();

	private const double MaxDeleteTimeDiff = 5.0;

	private double _lastDeleteTime;

	protected override CloseMode OnEscape => CloseMode.Current;

	public void OpenSaveMenu()
	{
		OpenSaveMenu(CloseMode.Current);
	}

	public void OpenSaveMenu(CloseMode cancelCloseMode)
	{
		if (implementation.CanSave(MsgDrawer.main))
		{
			Menu.textInput.Open(Loc.main.Cancel, Loc.main.Save, CreateSave, CloseMode.Current, cancelCloseMode, TextInputMenu.Element(string.Empty, string.Empty));
		}
		else
		{
			ScreenManager.main.CloseStack();
		}
		void CreateSave(string[] saveName)
		{
			implementation.Save(saveName[0]);
		}
	}

	private void Start()
	{
		ReorderingGroup obj = reorderingGroup;
		obj.onReorder = (Action<ReorderingModule>)Delegate.Combine(obj.onReorder, new Action<ReorderingModule>(OnFileReordered));
	}

	public void Load()
	{
		if (GetSelected(out var selected))
		{
			MsgDrawer.main.Log(Loc.main.Loading_In_Progress);
			ScreenManager.main.CloseStack();
			implementation.Load(selected.text.Text);
		}
	}

	public void Rename()
	{
		if (GetSelected(out var selected))
		{
			string selectedName = selected.text.Text;
			Menu.textInput.Open(Loc.main.Cancel, Loc.main.Rename, delegate(string[] input)
			{
				implementation.Rename(selectedName, input[0]);
				ReloadElements();
			}, CloseMode.Current, TextInputMenu.Element(string.Empty, selectedName));
		}
	}

	public void Import()
	{
		if (GetSelected(out var selected))
		{
			MsgDrawer.main.Log(Loc.main.Importing_In_Progress);
			ScreenManager.main.CloseStack();
			implementation.Import(selected.text.Text);
		}
	}

	public void Delete()
	{
		if (!GetSelected(out var selected))
		{
			return;
		}
		string name = string.Copy(selected.text.Text);
		if (_lastDeleteTime == 0.0 || Time.realtimeSinceStartupAsDouble - _lastDeleteTime > 5.0)
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => string.Concat(Loc.main.Delete_File_With_Type.Inject(name, "filename").InjectField((implementation.Title == Loc.main.Blueprints_Menu_Title) ? Loc.main.Blueprint : Loc.main.Quicksave, "filetype"), "?"), () => Loc.main.Delete, delegate
			{
				Delete(name);
			}, () => Loc.main.Cancel);
		}
		else
		{
			Delete(name);
		}
	}

	private void Delete(string name)
	{
		_lastDeleteTime = Time.realtimeSinceStartupAsDouble;
		implementation.Delete(name);
		ReloadElements();
	}

	private void ReloadElements()
	{
		foreach (LoadMenuElement element in elements)
		{
			UnityEngine.Object.DestroyImmediate(element.gameObject);
		}
		elements.Clear();
		foreach (string item in implementation.GetOrderer().GetOrder())
		{
			LoadMenuElement loadMenuElement = UnityEngine.Object.Instantiate(elementPrefab, loadFilesHolder);
			loadMenuElement.text.Text = item;
			loadMenuElement.radioButton.group = savesGroup;
			elements.Add(loadMenuElement);
			scrollElement.RegisterScrolling(loadMenuElement.button, () => !reorderingGroup.holding);
			reorderingGroup.Add(loadMenuElement.reorderingModule);
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(loadFilesHolder);
	}

	private void OnFileReordered(ReorderingModule elementReorderingModule)
	{
		LoadMenuElement loadMenuElement = elements.FirstOrDefault((LoadMenuElement e) => e.reorderingModule == elementReorderingModule);
		if (loadMenuElement == null)
		{
			return;
		}
		OrderedPathList orderer = implementation.GetOrderer();
		foreach (string item in orderer.GetOrder())
		{
			if (item == loadMenuElement.text.Text)
			{
				int siblingIndex = loadMenuElement.transform.GetSiblingIndex();
				orderer.Move(item, siblingIndex);
			}
		}
	}

	private bool GetSelected(out LoadMenuElement selected)
	{
		selected = elements.Find((LoadMenuElement element) => element.button == savesGroup.SelectedButton);
		return selected != null;
	}

	private void Update()
	{
		bool selected = GetSelected(out var _);
		deleteButton.SetEnabled(selected);
		loadButton.SetEnabled(selected);
		renameButton.SetEnabled(selected);
		UpdateImportButton(selected);
	}

	private void UpdateImportButton(bool hasMadeSelection)
	{
		if (implementation != null)
		{
			ImportAvailability importAvailability = implementation.GetImportAvailability();
			bool active = true;
			bool flag = hasMadeSelection;
			switch (importAvailability)
			{
			case ImportAvailability.NotAvailable:
				active = false;
				break;
			case ImportAvailability.ButtonDisabled:
				flag = false;
				break;
			}
			importButton.gameObject.SetActive(active);
			importButton.SetEnabled(flag);
		}
	}

	public override void OnOpen()
	{
		UpdateImportButton(hasMadeSelection: false);
		holder.gameObject.SetActive(value: true);
		title.Text = implementation.Title;
		loadButtonText.Text = implementation.LoadButtonText;
		ReloadElements();
	}

	public override void OnClose()
	{
		holder.gameObject.SetActive(value: false);
	}
}
