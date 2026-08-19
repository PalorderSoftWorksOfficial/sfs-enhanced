using System;
using System.Collections.Generic;
using SFS.UI;
using UnityEngine;

namespace SFS.Builds;

public class PickCategoriesMenu : MonoBehaviour
{
	public ExpandMenu expandMenu;

	public PickCategoryUI pickCategoryPrefab;

	public SizeSyncGroup syncGroup;

	public RectTransform buyPartsButton;

	private Dictionary<PickGridUI.CategoryParts, PickCategoryUI> elements = new Dictionary<PickGridUI.CategoryParts, PickCategoryUI>();

	private PickGridUI.CategoryParts selected;

	public event Action onMenuOpen;

	private void Start()
	{
		expandMenu.expanded.OnChange += (Action)delegate
		{
			base.gameObject.SetActive(expandMenu.expanded.Value);
			if (expandMenu.expanded.Value)
			{
				onMenuOpen?.Invoke();
			}
		};
	}

	public void SetupElements(PickGridUI.CategoryParts[] picklists)
	{
		foreach (PickCategoryUI value in elements.Values)
		{
			syncGroup.elements.Remove(value.size);
			UnityEngine.Object.Destroy(value.gameObject);
		}
		elements.Clear();
		foreach (PickGridUI.CategoryParts categoryParts in picklists)
		{
			PickCategoryUI element = UnityEngine.Object.Instantiate(pickCategoryPrefab, base.transform);
			element.list = categoryParts;
			element.text.Text = categoryParts.tag.displayName.Field;
			element.button.onClick += (Action)delegate
			{
				SelectCategory(element.list);
			};
			element.size.syncGroup = syncGroup;
			syncGroup.elements.Add(element.size);
			elements.Add(categoryParts, element);
			if (!DevSettings.FullVersion)
			{
				break;
			}
		}
		if (!DevSettings.FullVersion)
		{
			PickCategoryUI pickCategoryUI = UnityEngine.Object.Instantiate(pickCategoryPrefab, base.transform);
			pickCategoryUI.text.Text = "Get Full Version";
			pickCategoryUI.button.onClick += (Action)delegate
			{
				Base.sceneLoader.LoadHomeScene("Full Version");
			};
			pickCategoryUI.size.syncGroup = syncGroup;
			syncGroup.elements.Add(pickCategoryUI.size);
		}
	}

	public void SelectCategory(PickGridUI.CategoryParts newSelected)
	{
		expandMenu.Close();
		if (selected != null && elements.ContainsKey(selected))
		{
			elements[selected].Selected(selected: false);
		}
		elements[newSelected].Selected(selected: true);
		selected = newSelected;
		BuildManager.main.pickGrid.OpenCategory(newSelected);
	}
}
