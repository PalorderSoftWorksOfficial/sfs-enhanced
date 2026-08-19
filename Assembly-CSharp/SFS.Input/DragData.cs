using SFS.Cameras;
using UnityEngine;

namespace SFS.Input;

public class DragData
{
	public readonly Vector2 deltaPixel;

	public DragData(Vector2 deltaPixel)
	{
		this.deltaPixel = deltaPixel;
	}

	public Vector2 DeltaWorld(float positionZ)
	{
		float z = 0f - (ActiveCamera.Camera.transform.position.z - positionZ);
		Vector3 position = new Vector3(0f, 0f, z);
		Vector3 position2 = new Vector3(deltaPixel.x, deltaPixel.y, z);
		return ActiveCamera.Camera.camera.ScreenToWorldPoint(position2) - ActiveCamera.Camera.camera.ScreenToWorldPoint(position);
	}
}
