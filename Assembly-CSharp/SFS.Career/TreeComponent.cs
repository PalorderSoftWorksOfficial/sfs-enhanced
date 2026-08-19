using System;
using System.Collections;
using SFS.Input;
using SFS.UI;
using UnityEngine;

namespace SFS.Career;

public class TreeComponent : MonoBehaviour
{
	[Serializable]
	public class Root
	{
		public string codeName;

		public Vector2 position;
	}

	public RectTransform treeHolder;

	public RectTransform selectedHolder;

	public Button tabBackground;

	public ScrollElement viewScroller;

	[Space]
	public Root[] roots;

	private TT_Base selected;

	private Transform selected_OriginalHolder;

	private Vector2 selected_OriginalPosition;

	private float selected_OriginalHolderScale;

	private bool animating;

	private void Start()
	{
		viewScroller.RegisterScrolling(tabBackground);
		tabBackground.onZoom += (Action<ZoomData>)delegate(ZoomData data)
		{
			if (!animating)
			{
				treeHolder.localScale = Vector3.one * Mathf.Clamp(treeHolder.localScale.x / data.zoomDelta, 0.35f, 0.9f);
				Vector2 vector = treeHolder.InverseTransformPoint(data.zoomPosition.pixel);
				treeHolder.localPosition += treeHolder.parent.InverseTransformVector(data.zoomPosition.pixel - (Vector2)treeHolder.TransformPoint(vector));
			}
		};
		tabBackground.onClick += (Action<OnInputEndData>)delegate(OnInputEndData data)
		{
			if (data.inputType == InputType.Touch || data.inputType == InputType.MouseLeft)
			{
				ToggleSelect(null);
			}
		};
	}

	public void ToggleSelect(TT_Base selectedNew, float delay = 0f)
	{
		if (!animating)
		{
			if (selected != null)
			{
				StartCoroutine(DeselectAnimation(delay));
			}
			else if (selectedNew != null)
			{
				selected = selectedNew;
				selected.OnSelect();
				StartCoroutine(SelectAnimation(delay));
			}
		}
	}

	private IEnumerator SelectAnimation(float delay)
	{
		animating = true;
		yield return new WaitForSeconds(delay);
		selected_OriginalHolder = selected.transform.parent;
		selected_OriginalPosition = selected.transform.localPosition;
		selected_OriginalHolderScale = selected_OriginalHolder.localScale.x;
		float startScale = selected_OriginalHolder.localScale.x;
		Vector2 startPosition = selected_OriginalHolder.localPosition;
		selected_OriginalHolder.localScale = Vector3.one;
		Vector2 targetPosition = startPosition + (Vector2)selected_OriginalHolder.parent.InverseTransformVector(selectedHolder.transform.position - selected.transform.position);
		float transitionTime = 0.4f + Vector2.Distance(startPosition, targetPosition) / 5000f;
		float t = 0f;
		do
		{
			t += Time.unscaledDeltaTime / transitionTime;
			float t2 = t * t / (2f * (t * t - t) + 1f);
			selected_OriginalHolder.localPosition = Vector2.Lerp(startPosition, targetPosition, t2);
			selected_OriginalHolder.localScale = Vector3.one * Mathf.Lerp(startScale, selectedHolder.localScale.x, t2);
			yield return new WaitForEndOfFrame();
		}
		while (t < 1f);
		selected_OriginalHolder.gameObject.SetActive(value: false);
		selected.transform.SetParent(selectedHolder);
		selected.transform.localPosition = Vector3.zero;
		selected.transform.localScale = Vector3.one;
		yield return new WaitForSeconds(0.25f);
		if (selected.unselectedHolder != null)
		{
			selected.unselectedHolder.SetActive(value: false);
			yield return new WaitForSeconds(0.1f);
		}
		if (selected.selectedHolder != null)
		{
			selected.selectedHolder.SetActive(value: true);
			yield return new WaitForSeconds(0.2f);
		}
		animating = false;
	}

	private IEnumerator DeselectAnimation(float delay)
	{
		animating = true;
		yield return new WaitForSeconds(delay);
		if (selected.selectedHolder != null)
		{
			if (selected.selectedHolder.GetComponent<PopupAnimation>() != null)
			{
				selected.selectedHolder.GetComponent<PopupAnimation>().Disable();
			}
			else
			{
				selected.selectedHolder.SetActive(value: false);
			}
		}
		yield return new WaitForSeconds(0.4f);
		if (selected.unselectedHolder != null)
		{
			selected.unselectedHolder.SetActive(value: true);
			yield return new WaitForSeconds(0.1f);
		}
		selected_OriginalHolder.gameObject.SetActive(value: true);
		selected.transform.SetParent(selected_OriginalHolder);
		selected.transform.localPosition = selected_OriginalPosition;
		selected_OriginalHolderScale = Mathf.Min(selected_OriginalHolderScale, 0.8f);
		Vector2 startPosition = selected_OriginalHolder.localPosition;
		selected_OriginalHolder.localScale = Vector3.one * selected_OriginalHolderScale;
		Vector2 targetPosition = startPosition + (Vector2)selected_OriginalHolder.parent.InverseTransformVector(selectedHolder.transform.position - selected.transform.position);
		float t = 0f;
		do
		{
			t += Time.unscaledDeltaTime / 0.35f;
			float t2 = t * t / (2f * (t * t - t) + 1f);
			selected_OriginalHolder.localPosition = Vector2.Lerp(startPosition, targetPosition, t2);
			selected_OriginalHolder.localScale = Vector3.one * Mathf.Lerp(selectedHolder.localScale.x, selected_OriginalHolderScale, t2);
			yield return new WaitForEndOfFrame();
		}
		while (t < 1f);
		selected = null;
		animating = false;
	}
}
