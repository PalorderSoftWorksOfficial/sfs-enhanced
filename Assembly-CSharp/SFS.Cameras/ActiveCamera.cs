using System;
using UnityEngine;

namespace SFS.Cameras;

public class ActiveCamera : MonoBehaviour
{
	public static ActiveCamera main;

	public CameraManager_Local activeCamera;

	public static CameraManager Camera
	{
		get
		{
			return main.activeCamera;
		}
		set
		{
			main.activeCamera.Value = value;
		}
	}

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		activeCamera.OnChange += new Action<CameraManager, CameraManager>(OnActiveCameraChange);
	}

	private static void OnActiveCameraChange(CameraManager oldValue, CameraManager newValue)
	{
		if (oldValue != null)
		{
			oldValue.gameObject.SetActive(value: false);
		}
		if (newValue != null)
		{
			newValue.gameObject.SetActive(value: true);
		}
	}
}
