using System;
using System.Collections.Generic;
using SFS.Cameras;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class ElementDrawer : MonoBehaviour
{
	[Serializable]
	public class Element
	{
		public int priority;

		public Vector2 size;

		public Vector2 position;

		public TextMesh textMesh;
	}

	[Serializable]
	public class TextElement
	{
		public TextMesh textMesh;

		public Renderer renderer;
	}

	public float textSize = 0.004f;

	public Transform prefab;

	public List<Element> elements;

	public List<TextElement> textElements = new List<TextElement>();

	public int listIndex;

	private void Start()
	{
		Map.view.view.distance.OnChange += new Action(UpdateTextSize);
	}

	private void UpdateTextSize()
	{
		Vector3 localScale = Vector3.one * Map.view.ToConstantSize(textSize);
		foreach (TextElement textElement in textElements)
		{
			textElement.textMesh.transform.localScale = localScale;
		}
	}

	public void DrawTextElement(string text, Vector2 anchorNormal, int fontSize, Color color, Vector2 position, int priority, bool clearBelow, int renderOrder)
	{
		if (!(color.a <= 0f))
		{
			anchorNormal = Double2.ToDouble2(anchorNormal).Rotate(0f - ActiveCamera.Camera.CameraRotationRadians);
			if (listIndex > textElements.Count - 1)
			{
				CreateNewElement();
			}
			TextMesh textMesh = textElements[listIndex].textMesh;
			textMesh.text = GetTextAnchorFromDirection(anchorNormal, text);
			textMesh.anchor = GetTextAnchorFromDirection(anchorNormal);
			textMesh.fontSize = fontSize;
			textMesh.color = color;
			textMesh.transform.position = (Vector3)position + new Vector3(0f, 0f, (float)((double)Map.view.view.distance / 1000.0));
			textElements[listIndex].renderer.sortingOrder = renderOrder;
			if (!textMesh.gameObject.activeSelf)
			{
				textMesh.gameObject.SetActive(value: true);
			}
			textMesh.transform.rotation = Quaternion.identity;
			RegisterElement(priority, textElements[listIndex].renderer.bounds.extents * 0.85f, position, textMesh, clearBelow);
			textMesh.transform.localEulerAngles = GameCamerasManager.main.map_Camera.transform.localEulerAngles;
			listIndex++;
		}
	}

	public void RegisterElement(int priority, Vector2 size, Vector2 position, TextMesh textMesh, bool clearBelow)
	{
		Element element = new Element
		{
			priority = priority,
			size = size,
			position = position,
			textMesh = textMesh
		};
		if (!clearBelow)
		{
			return;
		}
		float num = Map.view.ToConstantSize(0.01f);
		foreach (Element element2 in elements)
		{
			Vector2 a = element.position - element2.position;
			a = a.Rotate_Radians(0f - GameCamerasManager.main.map_Camera.CameraRotationRadians);
			float num2 = Mathf.Abs(a.x) - element.size.x - element2.size.x;
			if (num2 > num)
			{
				continue;
			}
			float num3 = Mathf.Abs(a.y) - element.size.y - element2.size.y;
			if (num3 > num)
			{
				continue;
			}
			float num4 = Mathf.Max(num2, num3) / num;
			if (element.priority > element2.priority)
			{
				if (element2.textMesh != null)
				{
					if (num4 > 0f)
					{
						element2.textMesh.color = new Color(1f, 1f, 1f, num4);
					}
					else if (element2.textMesh.gameObject.activeSelf)
					{
						element2.textMesh.gameObject.SetActive(value: false);
					}
				}
			}
			else if (element.priority < element2.priority && element.textMesh != null)
			{
				if (num4 > 0f)
				{
					element.textMesh.color = new Color(1f, 1f, 1f, num4);
				}
				else if (element.textMesh.gameObject.activeSelf)
				{
					element.textMesh.gameObject.SetActive(value: false);
				}
			}
		}
		elements.Add(element);
	}

	private void CreateNewElement()
	{
		TextMesh component = UnityEngine.Object.Instantiate(prefab).GetComponent<TextMesh>();
		component.transform.SetParent(base.transform);
		component.transform.localScale = Vector3.one * Map.view.ToConstantSize(0.004f);
		Renderer component2 = component.GetComponent<Renderer>();
		component2.sortingOrder = 3;
		component2.sortingLayerName = "Map";
		textElements.Add(new TextElement
		{
			textMesh = component,
			renderer = component2
		});
	}

	public void ResetElements()
	{
		elements.Clear();
		listIndex = 0;
		foreach (TextElement textElement in textElements)
		{
			if (textElement.textMesh.gameObject.activeSelf)
			{
				textElement.textMesh.gameObject.SetActive(value: false);
			}
		}
	}

	public static TextAnchor GetTextAnchorFromDirection(Vector2 normal)
	{
		if (normal.sqrMagnitude == 0f)
		{
			return TextAnchor.MiddleCenter;
		}
		if (Mathf.Abs(normal.x * 2f) > Mathf.Abs(normal.y))
		{
			if (!(normal.x > 0f))
			{
				return TextAnchor.MiddleRight;
			}
			return TextAnchor.MiddleLeft;
		}
		if (!(normal.y > 0f))
		{
			return TextAnchor.MiddleCenter;
		}
		return TextAnchor.MiddleCenter;
	}

	public static string GetTextAnchorFromDirection(Vector2 normal, string text)
	{
		if (normal.sqrMagnitude == 0f)
		{
			return text;
		}
		if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
		{
			if (!(normal.x > 0f))
			{
				return text + "  ";
			}
			return "  " + text;
		}
		if (!(normal.y > 0f))
		{
			return "\n\n" + text;
		}
		return text + "\n\n";
	}
}
