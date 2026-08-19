using System;
using SFS.Cameras;
using SFS.UI;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class BuildCamera : MonoBehaviour
{
	public CameraManager cameraManager;

	public float minCameraDistance;

	public float maxCameraDistance;

	public float positionRubberbandingDistance;

	public float zoomRubberbandingDistance;

	public float rubberbandingTime;

	public bool dragging;

	private Vector2 velocity;

	private float zoomVelocity;

	private float MaxCameraDistance => maxCameraDistance * (float)((!SandboxSettings.main.settings.infiniteBuildArea) ? 1 : 2);

	public Vector2 CameraPosition
	{
		get
		{
			return cameraManager.position;
		}
		set
		{
			if (SandboxSettings.main.settings.infiniteBuildArea)
			{
				cameraManager.position.Value = value;
				return;
			}
			float x = Rubberbanding.Clamp(cameraManager.position.Value.x, value.x, 0f, BuildManager.main.buildGrid.gridSize.size.Value.x, positionRubberbandingDistance);
			float y = Rubberbanding.Clamp(cameraManager.position.Value.y, value.y, 0f, BuildManager.main.buildGrid.gridSize.size.Value.y, positionRubberbandingDistance);
			cameraManager.position.Value = new Vector2(x, y);
		}
	}

	public float CameraDistance
	{
		get
		{
			return cameraManager.distance;
		}
		set
		{
			cameraManager.distance.Value = Mathf.Clamp(value, minCameraDistance, MaxCameraDistance);
		}
	}

	private void Start()
	{
		SandboxSettings.main.onToggleCheat += new Action(OnCheatsChange);
	}

	private void OnDestroy()
	{
		SandboxSettings.main.onToggleCheat -= new Action(OnCheatsChange);
	}

	private void OnCheatsChange()
	{
		CameraDistance = CameraDistance;
	}

	private void LateUpdate()
	{
		if (!dragging && !SandboxSettings.main.settings.infiniteBuildArea)
		{
			float x = Rubberbanding.Rubberband(cameraManager.position.Value.x, 0f, BuildManager.main.buildGrid.gridSize.size.Value.x, positionRubberbandingDistance, rubberbandingTime, ref velocity.x, out var outOfBounds);
			float y = Rubberbanding.Rubberband(cameraManager.position.Value.y, 0f, BuildManager.main.buildGrid.gridSize.size.Value.y, positionRubberbandingDistance, rubberbandingTime, ref velocity.y, out outOfBounds);
			cameraManager.position.Value = new Vector2(x, y);
		}
	}
}
