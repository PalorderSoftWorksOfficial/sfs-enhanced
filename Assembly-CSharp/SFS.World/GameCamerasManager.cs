using System;
using SFS.Cameras;
using SFS.World.Maps;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class GameCamerasManager : MonoBehaviour
{
	public static GameCamerasManager main;

	public CameraManager world_Camera;

	public CameraManager scaledWorld_Camera;

	public CameraManager map_Camera;

	private float cameraAngle;

	private float cameraAngularVelocity;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		Map.manager.mapMode.OnChange += new Action(UpdateActiveCamera);
		WorldView.main.scaledSpace.OnChange += new Action(UpdateActiveCamera);
		WorldView worldView = WorldView.main;
		worldView.onUpdate = (Action)Delegate.Combine(worldView.onUpdate, new Action(OnUpdate));
	}

	private void UpdateActiveCamera()
	{
		ActiveCamera.Camera = (Map.manager.mapMode ? map_Camera : (WorldView.main.scaledSpace ? scaledWorld_Camera : world_Camera));
	}

	private void OnUpdate()
	{
		if (Time.deltaTime > 0.0001f)
		{
			SetCameraAngle(Mathf.SmoothDampAngle(cameraAngle, GetTargetCameraAngle(), ref cameraAngularVelocity, 0.5f));
		}
	}

	public void InstantlyRotateCamera()
	{
		SetCameraAngle(GetTargetCameraAngle());
		cameraAngularVelocity = 0f;
	}

	private static float GetTargetCameraAngle()
	{
		Location viewLocation = WorldView.main.ViewLocation;
		if (!viewLocation.planet.data.basics.rotateCamera || !viewLocation.position.Mag_LessThan(Planet.GetTimewarpRadius_AscendDescend(viewLocation)))
		{
			return 0f;
		}
		return (float)viewLocation.position.AngleDegrees - 90f;
	}

	private void SetCameraAngle(float newCameraAngle)
	{
		cameraAngle = newCameraAngle;
		CameraManager[] array = new CameraManager[3] { world_Camera, scaledWorld_Camera, map_Camera };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].rotation.Value = newCameraAngle;
		}
	}
}
