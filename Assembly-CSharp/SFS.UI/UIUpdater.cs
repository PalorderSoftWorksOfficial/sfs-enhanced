using UnityEngine;

namespace SFS.UI;

public class UIUpdater : MonoBehaviour
{
	public NewElement element;

	public bool screenSizeChange;

	public bool rectChange;

	public bool rotationChange;

	public bool enable;

	[Space]
	public bool unityUpdate;

	public bool unityLateUpdate;

	private Vector2 oldScreenSize;

	private bool updatedScreenSize;

	private ScreenOrientation? oldOrientation;

	private void Awake()
	{
		oldScreenSize = new Vector2(Screen.width, Screen.height);
	}

	private void OnEnable()
	{
		if (enable)
		{
			element.UpdateHierarchy();
		}
	}

	private void OnRectTransformDimensionsChange()
	{
		if (rectChange)
		{
			element.UpdateHierarchy();
		}
	}

	private void Update()
	{
		if (unityUpdate)
		{
			element.UpdateHierarchy();
		}
		if (!rotationChange)
		{
			return;
		}
		if (!oldOrientation.HasValue)
		{
			oldOrientation = Screen.orientation;
		}
		if (Screen.orientation != oldOrientation)
		{
			oldOrientation = Screen.orientation;
			element.UpdateHierarchy();
		}
		if (updatedScreenSize)
		{
			updatedScreenSize = false;
			element.UpdateHierarchy();
		}
		if (screenSizeChange)
		{
			Vector2 vector = new Vector2(Screen.width, Screen.height);
			if (oldScreenSize != vector)
			{
				oldScreenSize = vector;
				updatedScreenSize = true;
			}
		}
	}

	private void LateUpdate()
	{
		if (unityLateUpdate)
		{
			element.UpdateHierarchy();
		}
	}
}
