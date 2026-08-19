using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SFS.Input;

public static class TouchElements
{
	public static List<I_Touchable> elements = new List<I_Touchable>();

	private static Dictionary<Transform, Canvas> transformCanvases = new Dictionary<Transform, Canvas>();

	public static I_Touchable RaycastElements(TouchPosition touchPosition)
	{
		elements.Sort(SortCompare);
		for (int i = 0; i < elements.Count; i++)
		{
			I_Touchable i_Touchable = elements[i];
			if (i_Touchable.PointcastElement(touchPosition))
			{
				if (!(i_Touchable is SkipUI skipUI))
				{
					return i_Touchable;
				}
				for (List<int> priority = GetPriority(skipUI.skipEnd); elements.IsValidIndex(i + 1) && SortCompare(priority, elements[i + 1].GetPriority()) > 0; i++)
				{
				}
			}
		}
		return null;
	}

	public static Button RaycastElements_MouseScroll(TouchPosition touchPosition)
	{
		foreach (I_Touchable element in elements)
		{
			if (element is Button button && !button.onScroll.IsEmpty && element.PointcastElement(touchPosition))
			{
				return button;
			}
		}
		return null;
	}

	public static void AddElement(I_Touchable element)
	{
		elements.Add(element);
		elements.Sort(SortCompare);
		if (element is Component component)
		{
			RectTransform component2 = component.GetComponent<RectTransform>();
			if (component2 != null)
			{
				transformCanvases[component2] = component2.GetComponentInParent<Canvas>();
			}
		}
	}

	public static void RemoveElement(I_Touchable element)
	{
		elements.Remove(element);
		if (element is Component component)
		{
			RectTransform component2 = component.GetComponent<RectTransform>();
			if (component2 != null)
			{
				transformCanvases.Remove(component2);
			}
		}
	}

	private static int SortCompare(I_Touchable a, I_Touchable b)
	{
		int num = ((a is Button button) ? button.layoutPriority : 0);
		int num2 = ((b is Button button2) ? button2.layoutPriority : 0);
		if (num != num2)
		{
			if (num <= num2)
			{
				return 1;
			}
			return -1;
		}
		return SortCompare(a.GetPriority(), b.GetPriority());
	}

	private static int SortCompare(List<int> path_A, List<int> path_B)
	{
		for (int i = 0; i < path_A.Count && i < path_B.Count; i++)
		{
			if (path_B[i] != path_A[i])
			{
				return path_B[i] - path_A[i];
			}
		}
		return path_B.Count - path_A.Count;
	}

	public static List<int> GetPriority(Transform a)
	{
		Stack<int> stack = new Stack<int>();
		stack.Push(a.GetSiblingIndex());
		while (a.parent != null)
		{
			a = a.parent;
			stack.Push(a.GetSiblingIndex());
		}
		for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
		{
			if (a.gameObject.scene == SceneManager.GetSceneByBuildIndex(i))
			{
				stack.Push(i);
			}
		}
		return stack.ToList();
	}

	public static bool PointcastElement(TouchPosition position, Transform a, bool advancedBorder, float border, float top, float bottom, float left, float right)
	{
		RectTransform component = a.GetComponent<RectTransform>();
		if (component == null)
		{
			return false;
		}
		Rect rect = component.rect;
		rect.size += (advancedBorder ? new Vector2(left + right, top + bottom) : new Vector2(border * 2f, border * 2f));
		if (PickGridPositioner.main != null && transformCanvases.TryGetValue(a, out var value) && value != null && value.renderMode == RenderMode.ScreenSpaceCamera)
		{
			Vector3 position2 = PickGridPositioner.main.SetZ_ScreenSpaceToWorldSpace(position.pixel, a.position.z);
			return rect.Contains(a.InverseTransformPoint(position2));
		}
		return rect.Contains(a.InverseTransformPoint(position.pixel));
	}
}
