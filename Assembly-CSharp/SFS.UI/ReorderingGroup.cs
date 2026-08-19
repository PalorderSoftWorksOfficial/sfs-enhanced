using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class ReorderingGroup : MonoBehaviour
{
	public bool elementsDifferInSize;

	public UnityEngine.UI.LayoutGroup layoutGroup;

	public RectTransform heldHolder;

	public RectTransform positionIndicator;

	public ElementLayout layoutMode;

	public Vector2 holdOffset;

	public int extraElements;

	public ScrollElement scrollElement;

	public bool scrollHorizontal;

	public bool scrollVertical;

	public float scrollSpeed = 200f;

	public float scrollAreaMargin;

	[HideInInspector]
	public ReorderingModule holding;

	private List<ReorderingModule> elements = new List<ReorderingModule>();

	public Action<ReorderingModule> onReorder;

	public void Add(ReorderingModule element)
	{
		elements.Add(element);
		element.owner = this;
	}

	public void Remove(ReorderingModule element)
	{
		if (elements.Contains(element))
		{
			elements.Remove(element);
			element.owner = null;
		}
	}

	public void ActivatePositionIndicator(int siblingIndex, Vector3 position, Vector3 size)
	{
		positionIndicator.gameObject.SetActive(value: true);
		positionIndicator.SetParent(base.transform);
		positionIndicator.SetSiblingIndex(siblingIndex);
		if (positionIndicator.GetComponent<LayoutElement>() != null)
		{
			positionIndicator.GetComponent<LayoutElement>().preferredWidth = size.x;
			positionIndicator.GetComponent<LayoutElement>().preferredHeight = size.y;
		}
		else
		{
			positionIndicator.sizeDelta = size;
		}
		positionIndicator.position = position;
	}

	public int GetIndicatorSiblingIndex(ReorderingModule draggedElement)
	{
		if (elementsDifferInSize)
		{
			float num = float.PositiveInfinity;
			int result = extraElements;
			for (int i = extraElements; i < elements.Count + extraElements; i++)
			{
				positionIndicator.SetSiblingIndex(i);
				LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetRect());
				float num2 = Vector2.Distance(positionIndicator.transform.position, draggedElement.transform.position);
				if (num2 < num)
				{
					result = i;
					num = num2;
				}
			}
			return result;
		}
		ReorderingModule reorderingModule = null;
		float num3 = Vector2.Distance(positionIndicator.position, draggedElement.Rect.position);
		foreach (ReorderingModule element in elements)
		{
			if (!(element == draggedElement))
			{
				float num4 = Vector2.Distance(element.Rect.position, draggedElement.Rect.position);
				if (num4 < num3)
				{
					reorderingModule = element;
					num3 = num4;
				}
			}
		}
		if (reorderingModule != null)
		{
			return reorderingModule.Rect.GetSiblingIndex();
		}
		return -1;
	}

	public void DeactivatePositionIndicator()
	{
		positionIndicator.SetParent(base.transform.parent);
		positionIndicator.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		if (holding != null)
		{
			holding.OnDrop();
		}
	}
}
