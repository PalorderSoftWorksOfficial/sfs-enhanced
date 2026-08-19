using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Cameras;

[RequireComponent(typeof(Camera))]
public class CameraManager : ObservableMonoBehaviour
{
	[HideInInspector]
	public Camera camera;

	public Vector2_Local position;

	public Float_Local distance;

	public Float_Local rotation;

	private Vector2Int resolution;

	public float CameraRotationRadians => CameraRotationDegrees * ((float)Math.PI / 180f);

	public float CameraRotationDegrees => base.transform.eulerAngles.z;

	public float ViewSizeNormal => (GetView(0f, 0f) - GetView(1f, 1f)).magnitude / 2f;

	private void Awake()
	{
		camera = GetComponent<Camera>();
	}

	private void Start()
	{
		position.OnChange += new Action(PositionCamera);
		distance.OnChange += new Action(PositionCamera);
		rotation.OnChange += new Action(RotateCamera);
	}

	private new void OnDestroy()
	{
		position.OnChange -= new Action(PositionCamera);
		distance.OnChange -= new Action(PositionCamera);
		rotation.OnChange -= new Action(RotateCamera);
		base.OnDestroy();
	}

	private void Update()
	{
		Vector2Int vector2Int = new Vector2Int(Screen.width, Screen.height);
		if (resolution != vector2Int)
		{
			resolution = vector2Int;
			PositionCamera();
			RotateCamera();
		}
	}

	private void PositionCamera()
	{
		if (camera.orthographic)
		{
			float f = Mathf.Tan(0.5f * camera.fieldOfView * ((float)Math.PI / 180f)) * distance.Value;
			base.transform.position = (Vector3)position.Value + Vector3.back * 100f;
			camera.orthographicSize = Mathf.Abs(f);
		}
		else
		{
			base.transform.position = (Vector3)position.Value + Vector3.back * distance.Value;
		}
	}

	private void RotateCamera()
	{
		base.transform.eulerAngles = new Vector3(0f, 0f, rotation.Value);
	}

	private Vector2 GetView(float x, float y)
	{
		return camera.ScreenToWorldPoint(new Vector3((float)Screen.width * x, (float)Screen.height * y, 1f));
	}

	public (Vector2, Vector2) GetFramingData()
	{
		Vector2 item = GetView(0f, 0f) - GetView(1f, 0f);
		Vector2 item2 = GetView(0f, 0f) - GetView(0f, 1f);
		return (item, item2);
	}
}
