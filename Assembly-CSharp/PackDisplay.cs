using System;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

public class PackDisplay : MonoBehaviour
{
	[Serializable]
	public class DisplayPart
	{
		public Transform part;

		public Vector2 defaultPosition;

		public Vector2 expandedPosition;
	}

	public DisplayPart[] displayParts;

	public RectTransform frameBounds_A;

	public RectTransform frameBounds_B;

	public Camera mainCamera;

	public Transform expandedBounds_A;

	public Transform expandedBounds_B;

	public Transform pivot;

	public Transform holder;

	public float time;

	public float targetTime;

	public RectTransform contentFrame;

	public SpriteRenderer spriteRenderer;

	public Vector2 back_Pos_A;

	public Vector2 back_Pos_B;

	public Vector2 back_Size_A;

	public Vector2 back_Size_B;

	public float height_Default;

	public float height_Expanded;

	private bool initialized;

	private void LateUpdate()
	{
		time = Mathf.MoveTowards(time, targetTime, Time.unscaledDeltaTime * 5f);
		Position();
		holder.parent.rotation = mainCamera.transform.rotation;
		Vector2 vector = holder.parent.InverseTransformPoint(mainCamera.ScreenToWorldPoint(new Vector3(frameBounds_A.position.x, frameBounds_A.position.y, 20f)));
		Vector2 vector2 = holder.parent.InverseTransformPoint(mainCamera.ScreenToWorldPoint(new Vector3(frameBounds_B.position.x, frameBounds_B.position.y, 20f)));
		Vector2 vector3 = vector2 - vector;
		Vector2 vector4 = expandedBounds_A.localPosition;
		Vector2 vector5 = expandedBounds_B.localPosition;
		Vector2 vector6 = vector5 - vector4;
		float num = vector3.y / vector6.y;
		holder.localScale = Vector3.one * num;
		Vector2 vector7 = (Vector2)pivot.localPosition - new Vector2((vector4.x + vector5.x) / 2f, vector4.y);
		holder.localPosition = new Vector2((vector.x + vector2.x) / 2f, vector.y) + vector7 * num;
	}

	private void GetParts()
	{
		displayParts = (from a in holder.GetComponentsInChildren<Part>()
			select new DisplayPart
			{
				part = a.transform
			}).ToArray();
	}

	private void CopyToDefault()
	{
		DisplayPart[] array = displayParts;
		foreach (DisplayPart obj in array)
		{
			obj.defaultPosition = obj.part.localPosition;
		}
		back_Pos_A = spriteRenderer.transform.localPosition;
		back_Size_A = spriteRenderer.size;
	}

	private void CopyToExpanded()
	{
		DisplayPart[] array = displayParts;
		foreach (DisplayPart obj in array)
		{
			obj.expandedPosition = obj.part.localPosition;
		}
		back_Pos_B = spriteRenderer.transform.localPosition;
		back_Size_B = spriteRenderer.size;
	}

	private void Position()
	{
		DisplayPart[] array = displayParts;
		foreach (DisplayPart displayPart in array)
		{
			displayPart.part.localPosition = Vector2.LerpUnclamped(displayPart.defaultPosition, displayPart.expandedPosition, time);
		}
		contentFrame.sizeDelta = new Vector2(contentFrame.sizeDelta.x, Mathf.Lerp(height_Default, height_Expanded, time));
		contentFrame.gameObject.SetActive(value: false);
		contentFrame.gameObject.SetActive(value: true);
		spriteRenderer.transform.localPosition = Vector2.LerpUnclamped(back_Pos_A, back_Pos_B, time);
		spriteRenderer.size = Vector2.LerpUnclamped(back_Size_A, back_Size_B, time);
	}

	public void Toggle()
	{
		targetTime = ((targetTime != 1f) ? 1 : 0);
	}

	private void OnEnable()
	{
		if (!initialized)
		{
			initialized = true;
			DisplayPart[] array = displayParts;
			for (int i = 0; i < array.Length; i++)
			{
				BaseMesh[] componentsInChildren = array[i].part.GetComponentsInChildren<BaseMesh>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].GenerateMesh();
				}
			}
		}
		holder.gameObject.SetActive(value: true);
		time = 0f;
		targetTime = 0f;
		LateUpdate();
	}

	private void OnDisable()
	{
		holder.gameObject.SetActive(value: false);
	}
}
