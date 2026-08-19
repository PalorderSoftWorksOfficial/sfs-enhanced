using SFS.Cameras;
using UnityEngine;

namespace SFS;

public class CameraRotator : MonoBehaviour
{
	public CameraManager cameraManager;

	public float speed;

	private void Update()
	{
		cameraManager.rotation.Value += Time.unscaledDeltaTime * speed;
	}
}
