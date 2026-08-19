using SFS.Tween;
using SFS.UI;
using UnityEngine;

namespace SFS.Builds;

public class BuildOrientation : MonoBehaviour
{
	public static BuildOrientation main;

	public BuildCamera buildCamera;

	public AnimationCurve rotationCurve;

	public float rotation;

	public ButtonPC rotateButton;

	private ScreenOrientation screenOrientation;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		Update();
	}

	private void Update()
	{
	}

	public void ToggleOrientation()
	{
		SetOrientation((rotation == 0f) ? (-90) : 0, animate: true);
	}

	public void SetOrientation(float newRotation, bool animate)
	{
		float oldRotation = rotation;
		if (animate)
		{
			TweenManager.TweenFloat(0f, 1f, 0.5f, delegate(float t)
			{
				buildCamera.cameraManager.rotation.Value = Mathf.Lerp(oldRotation, newRotation, t);
			});
		}
		else
		{
			buildCamera.cameraManager.rotation.Value = 0f - newRotation;
		}
		rotateButton.SetSelected(newRotation != 0f);
		rotation = newRotation;
	}
}
