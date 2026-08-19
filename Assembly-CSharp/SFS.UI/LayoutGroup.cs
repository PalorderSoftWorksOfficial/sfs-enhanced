using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public abstract class LayoutGroup : NewElement
{
	public enum SpaceDistributionMode
	{
		TakeAvailable,
		DistributeEvenly,
		DistributeByUse
	}

	public HorizontalOrVerticalLayoutGroup layoutGroup;

	[SerializeField]
	private List<NewElement> elements = new List<NewElement>();

	public SizeMode sizeMode;

	public bool applySelfSizeX;

	public bool applySelfSizeY;

	public SpaceDistributionMode spaceDistributionMode;

	public bool takeAllAvailableSpace;

	[SerializeField]
	private RectOffset padding;

	[SerializeField]
	private float spacing;

	public int ElementCount => elements.Count;

	private bool HasGroup => layoutGroup != null;

	protected RectOffset Padding
	{
		get
		{
			if (!(layoutGroup != null))
			{
				return padding;
			}
			return layoutGroup.padding;
		}
	}

	public float Spacing
	{
		get
		{
			if (!(layoutGroup != null))
			{
				return spacing;
			}
			return layoutGroup.spacing;
		}
	}

	protected NewElement[] ActiveElements => elements.Where((NewElement e) => e.gameObject.activeSelf).ToArray();

	private void Awake()
	{
		foreach (NewElement element in elements)
		{
			element.SetParent(this);
		}
	}

	private void Reset()
	{
		layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
	}

	private void GetElements()
	{
		elements = (from a in GetComponentsInChildren<NewElement>()
			where a.transform.parent == base.transform
			select a).ToList();
	}

	public void AddElement(NewElement element)
	{
		elements.Add(element);
		element.SetParent(this);
	}

	public void RemoveElement(NewElement element)
	{
		elements.Remove(element);
	}

	public void RemoveElement(int index)
	{
		elements.RemoveAt(index);
	}

	public override void ActAsRoot()
	{
		SetSize(GetPreferredSize());
	}

	private Vector2 GetChildUnscaledPreferredCombinedSize()
	{
		Vector2 size = Vector2.zero;
		ActiveElements.ForEach(delegate(NewElement e)
		{
			size += e.GetPreferredSize();
		});
		return size;
	}

	protected Vector2[] GetSizeDistribution(out Vector2 preferredCombinedSize)
	{
		preferredCombinedSize = GetChildUnscaledPreferredCombinedSize();
		if (preferredCombinedSize.x == 0f)
		{
			preferredCombinedSize.x = 1f;
		}
		if (preferredCombinedSize.y == 0f)
		{
			preferredCombinedSize.y = 1f;
		}
		Vector2[] array = new Vector2[ActiveElements.Length];
		for (int i = 0; i < ActiveElements.Length; i++)
		{
			array[i] = ActiveElements[i].GetPreferredSize() / preferredCombinedSize;
		}
		return array;
	}

	protected Vector2 GetUsableSizeForChildren(Vector2 size, RectTransform.Axis elementAxis)
	{
		float num = (float)Mathf.Max(ActiveElements.Length - 1, 0) * Spacing;
		Vector2 vector = new Vector2((elementAxis == RectTransform.Axis.Horizontal) ? num : 0f, (elementAxis == RectTransform.Axis.Vertical) ? num : 0f);
		Vector2 vector2 = size - vector - new Vector2(Padding.horizontal, Padding.vertical);
		Vector2 size2 = (takeAllAvailableSpace ? vector2 : Vector2.Min(vector2, GetChildUnscaledPreferredCombinedSize()));
		return ApplyInsideModifiers(size2, -1);
	}
}
