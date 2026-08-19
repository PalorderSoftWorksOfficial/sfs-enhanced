using System;
using System.Collections;
using UnityEngine;

namespace SFS.UI;

public class ReorderingModule : MonoBehaviour
{
	public Button button;

	public bool instantPickupOnPC;

	[HideInInspector]
	public ReorderingGroup owner;

	private IEnumerator dragCoroutine;

	public RectTransform Rect => GetComponent<RectTransform>();

	private void Start()
	{
		if (instantPickupOnPC)
		{
			button.onDown += new Action(OnPickUp);
		}
		else
		{
			button.onLongClick += new Action(OnPickUp);
			if (button is ButtonPC buttonPC)
			{
				buttonPC.useLongClick = true;
			}
			else
			{
				Debug.LogWarning("PC button expected");
			}
		}
		button.onUp += new Action(OnDrop);
	}

	private void OnPickUp()
	{
		if (!(owner.holding != null))
		{
			owner.holding = this;
			dragCoroutine = Drag();
			StartCoroutine(dragCoroutine);
		}
	}

	public void OnDrop()
	{
		if (!(owner.holding != this))
		{
			owner.holding = null;
			StopCoroutine(dragCoroutine);
			OnFrameEnd main = OnFrameEnd.main;
			main.onBeforeFrameEnd_Once = (Action)Delegate.Combine(main.onBeforeFrameEnd_Once, (Action)delegate
			{
				Rect.SetParent(owner.transform);
				Rect.SetSiblingIndex(owner.positionIndicator.GetSiblingIndex());
				owner.DeactivatePositionIndicator();
				owner.onReorder?.Invoke(this);
			});
		}
	}

	private IEnumerator Drag()
	{
		owner.ActivatePositionIndicator(Rect.GetSiblingIndex(), Rect.position, Rect.sizeDelta);
		Rect.SetParent(owner.heldHolder);
		Vector2 startPosition = base.transform.position;
		float scrollSpeed = 0f;
		Vector2 scrollOffset = Vector2.zero;
		yield return new WaitForSecondsRealtime(0.1f);
		while (true)
		{
			Vector3 vector = startPosition;
			switch (owner.layoutMode)
			{
			case ElementLayout.Horizontal:
				vector.x = UnityEngine.Input.mousePosition.x;
				break;
			case ElementLayout.Vertical:
				vector.y = UnityEngine.Input.mousePosition.y;
				break;
			default:
				vector = UnityEngine.Input.mousePosition;
				break;
			}
			base.transform.position = vector + (Vector3)owner.holdOffset;
			int indicatorSiblingIndex = owner.GetIndicatorSiblingIndex(this);
			if (indicatorSiblingIndex != -1)
			{
				owner.positionIndicator.SetSiblingIndex(indicatorSiblingIndex);
			}
			if (owner.scrollElement != null)
			{
				Vector3[] array = new Vector3[4];
				owner.scrollElement.transform.parent.GetRect().GetWorldCorners(array);
				float num = owner.scrollAreaMargin * owner.transform.lossyScale.x;
				array[0] += Vector3.one * num;
				array[2] -= Vector3.one * num;
				Vector2 vector2 = new Vector2(owner.scrollHorizontal ? ((vector.x < array[0].x) ? 1 : ((vector.x > array[2].x) ? (-1) : 0)) : 0, owner.scrollVertical ? ((vector.y < array[0].y) ? 1 : ((vector.y > array[2].y) ? (-1) : 0)) : 0);
				scrollSpeed = Mathf.MoveTowards(scrollSpeed, (vector2 != Vector2.zero) ? 1 : 0, Time.unscaledDeltaTime / 0.2f);
				if (vector2 != Vector2.zero)
				{
					scrollOffset = vector2;
				}
				owner.scrollElement.Move(scrollOffset * owner.scrollSpeed * Time.unscaledDeltaTime * scrollSpeed);
			}
			yield return null;
		}
	}

	private void OnDestroy()
	{
		if (!(owner == null))
		{
			OnDrop();
			owner.Remove(this);
		}
	}
}
