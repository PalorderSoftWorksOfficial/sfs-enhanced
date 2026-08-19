using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class RelativeSizeFitter : MonoBehaviour
{
	[Serializable]
	public class Reference
	{
		public RectTransform reference;

		public float scale = 1f;
	}

	public List<Reference> horizontalReferences = new List<Reference>();

	public List<Reference> verticalReferences = new List<Reference>();

	public RectTransform[] layoutsToUpdate = new RectTransform[0];

	public SizeSyncGroup syncGroup;

	[SerializeField]
	[HideInInspector]
	private Vector2 offset;

	[SerializeField]
	[HideInInspector]
	private Vector2 minimumSize;

	private RectTransform rect;

	[HideInInspector]
	public Vector2 currentSize;

	public Vector2 Offset
	{
		get
		{
			return offset;
		}
		set
		{
			if (!(offset == value))
			{
				offset = value;
				UpdateUI();
			}
		}
	}

	public Vector2 MinimumSize
	{
		get
		{
			return minimumSize;
		}
		set
		{
			if (!(minimumSize == value))
			{
				minimumSize = value;
				UpdateUI();
			}
		}
	}

	private void Awake()
	{
		rect = GetComponent<RectTransform>();
		currentSize = rect.sizeDelta;
		UpdateUI();
	}

	private void Update()
	{
		UpdateUI();
	}

	public void UpdateUI()
	{
		currentSize = CalculateSize();
		if ((syncGroup != null && syncGroup.size != rect.sizeDelta) || rect.sizeDelta != currentSize)
		{
			rect.sizeDelta = ((syncGroup != null) ? syncGroup.size : currentSize);
			Canvas.ForceUpdateCanvases();
			RectTransform[] array = layoutsToUpdate;
			for (int i = 0; i < array.Length; i++)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(array[i]);
			}
		}
	}

	private Vector2 CalculateSize()
	{
		if (rect == null)
		{
			rect = GetComponent<RectTransform>();
		}
		Vector2 vector = Vector2.zero;
		if (horizontalReferences.Count > 0)
		{
			float num = horizontalReferences.Max((Reference h) => h.reference.sizeDelta.x * h.scale);
			vector = new Vector2(num + offset.x, rect.sizeDelta.y);
		}
		if (verticalReferences.Count > 0)
		{
			float num2 = verticalReferences.Max((Reference v) => v.reference.sizeDelta.y * v.scale);
			vector = new Vector2(rect.sizeDelta.x, num2 + offset.y);
		}
		return new Vector2((vector.x > minimumSize.x) ? vector.x : minimumSize.x, (vector.y > minimumSize.y) ? vector.y : minimumSize.y);
	}
}
