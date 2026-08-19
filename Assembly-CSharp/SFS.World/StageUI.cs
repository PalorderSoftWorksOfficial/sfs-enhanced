using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.World;

public class StageUI : MonoBehaviour
{
	public SFS.UI.Button button;

	public SFS.UI.Button[] removeButtons;

	public SFS.UI.Button[] duplicateButtons;

	public SFS.UI.Button[] useButtons;

	public GameObject selectedMark;

	public GameObject horizontalButtonsHolder_Right;

	public GameObject verticalButtonsHolder_Right;

	public GameObject horizontalButtonsHolder_Left;

	public GameObject verticalButtonsHolder_Left;

	public ReorderingModule elementOrderModule;

	public RawImage iconPrefab;

	public RectTransform iconsHolder;

	public TextAdapter stageIdText;

	public Stage stage;

	private List<GameObject> icons = new List<GameObject>();

	public void Initialize(Stage stage)
	{
		iconPrefab.gameObject.SetActive(value: false);
		this.stage = stage;
		for (int i = 0; i < stage.parts.Count; i++)
		{
			CreatePartIcon(stage.parts[i], i);
		}
		stage.onPartInserted = (Action<Part, int>)Delegate.Combine(stage.onPartInserted, new Action<Part, int>(CreatePartIcon));
		stage.onPartRemoved = (Action<int>)Delegate.Combine(stage.onPartRemoved, new Action<int>(DestroyPartIcon));
	}

	public void Destroy()
	{
		Stage obj = stage;
		obj.onPartInserted = (Action<Part, int>)Delegate.Remove(obj.onPartInserted, new Action<Part, int>(CreatePartIcon));
		Stage obj2 = stage;
		obj2.onPartRemoved = (Action<int>)Delegate.Remove(obj2.onPartRemoved, new Action<int>(DestroyPartIcon));
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void CreatePartIcon(Part part, int index)
	{
		Part part2 = (Base.partsLoader.parts.TryGetValue(part.name, out var value) ? value : null);
		PartSave a = new PartSave(part)
		{
			orientation = ((part2 != null) ? part2.orientation.orientation.Value.GetCopy() : new Orientation(1f, 1f, 0f))
		};
		int num = 150;
		num = 128;
		RenderTexture renderTexture = PartIconCreator.main.CreatePartIcon_Staging(a, num);
		RawImage rawImage = UnityEngine.Object.Instantiate(iconPrefab, iconsHolder);
		rawImage.gameObject.SetActive(value: true);
		rawImage.texture = renderTexture;
		rawImage.rectTransform.sizeDelta = new Vector2(renderTexture.width, renderTexture.height) / 2f;
		rawImage.transform.SetSiblingIndex(index + 1);
		icons.Insert(index, rawImage.gameObject);
	}

	private void DestroyPartIcon(int index)
	{
		UnityEngine.Object.Destroy(icons[index]);
		icons.RemoveAt(index);
	}

	private void Update()
	{
		Vector2 sizeDelta = iconsHolder.sizeDelta;
		sizeDelta.y = ((stage != null && stage.PartCount > 0) ? Mathf.Max(sizeDelta.y, 60f) : 60f);
		if (GameManager.main != null)
		{
			bool flag = sizeDelta.y > 130f;
			if (horizontalButtonsHolder_Left.activeSelf != !flag)
			{
				horizontalButtonsHolder_Left.SetActive(!flag);
			}
			if (verticalButtonsHolder_Left.activeSelf != flag)
			{
				verticalButtonsHolder_Left.SetActive(flag);
			}
		}
		if (BuildManager.main != null)
		{
			bool flag2 = sizeDelta.y > 130f;
			if (horizontalButtonsHolder_Right.activeSelf != !flag2)
			{
				horizontalButtonsHolder_Right.SetActive(!flag2);
			}
			if (verticalButtonsHolder_Right.activeSelf != flag2)
			{
				verticalButtonsHolder_Right.SetActive(flag2);
			}
		}
		RectTransform component = GetComponent<RectTransform>();
		if (component.sizeDelta != sizeDelta)
		{
			component.sizeDelta = sizeDelta;
			GetComponentInParent<VerticalLayoutGroup>().enabled = false;
			GetComponentInParent<VerticalLayoutGroup>().enabled = true;
		}
	}
}
