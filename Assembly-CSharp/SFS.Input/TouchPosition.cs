using SFS.Cameras;
using UnityEngine;

namespace SFS.Input;

public class TouchPosition
{
	public Vector2 pixel;

	public TouchPosition(Vector2 pixel)
	{
		this.pixel = pixel;
	}

	public Vector2 World(float positionZ)
	{
		float num = 0f - (ActiveCamera.Camera.transform.position.z - positionZ);
		return ActiveCamera.Camera.camera.ScreenToWorldPoint((Vector3)pixel + Vector3.forward * num);
	}
}
