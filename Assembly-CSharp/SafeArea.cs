using UnityEngine;

public class SafeArea : MonoBehaviour
{
	public enum HorizontalMode
	{
		Center,
		Full,
		Left,
		Right
	}

	public enum VerticalMode
	{
		Center,
		Full,
		Bottom,
		Top
	}

	public HorizontalMode horizontal;

	public VerticalMode vertical;

	private RectTransform panel;

	private Rect lastSafeArea;

	private Vector2Int lastScreenSize;

	private ScreenOrientation lastOrientation;

	private void Apply()
	{
		lastSafeArea = Rect.zero;
		Awake();
	}

	private void Awake()
	{
		panel = GetComponent<RectTransform>();
		Refresh();
	}

	private void Update()
	{
		Refresh();
	}

	private void OnEnable()
	{
		Refresh();
	}

	private void Refresh()
	{
		Rect safeArea = Screen.safeArea;
		Vector2Int vector2Int = new Vector2Int(Screen.width, Screen.height);
		if (!(safeArea == lastSafeArea) || !(vector2Int == lastScreenSize) || Screen.orientation != lastOrientation)
		{
			lastSafeArea = safeArea;
			lastScreenSize = vector2Int;
			lastOrientation = Screen.orientation;
			ApplySafeArea(safeArea, vector2Int);
		}
	}

	private void ApplySafeArea(Rect safeArea, Vector2 screenSize)
	{
		switch (horizontal)
		{
		case HorizontalMode.Full:
			safeArea.x = 0f;
			safeArea.width = screenSize.x;
			break;
		case HorizontalMode.Left:
			safeArea.width = safeArea.x;
			safeArea.x = 0f;
			break;
		case HorizontalMode.Right:
		{
			float xMax = safeArea.xMax;
			safeArea.width = screenSize.x - xMax;
			safeArea.x = xMax;
			break;
		}
		}
		switch (vertical)
		{
		case VerticalMode.Full:
			safeArea.y = 0f;
			safeArea.height = screenSize.y;
			break;
		case VerticalMode.Bottom:
			safeArea.height = safeArea.y;
			safeArea.y = 0f;
			break;
		case VerticalMode.Top:
		{
			float yMax = safeArea.yMax;
			safeArea.height = screenSize.y - yMax;
			safeArea.y = yMax;
			break;
		}
		}
		if (screenSize.sqrMagnitude != 0f)
		{
			Vector2 anchorMin = safeArea.position / screenSize;
			Vector2 anchorMax = (safeArea.position + safeArea.size) / screenSize;
			if (anchorMin.x >= 0f && anchorMin.y >= 0f && anchorMax.x >= 0f && anchorMax.y >= 0f)
			{
				panel.anchorMin = anchorMin;
				panel.anchorMax = anchorMax;
			}
		}
	}
}
