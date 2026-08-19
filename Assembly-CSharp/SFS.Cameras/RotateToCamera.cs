using SFS.Variables;
using UnityEngine;

namespace SFS.Cameras;

public class RotateToCamera : MonoBehaviour
{
	public Float_Local rotation;

	[Space]
	public bool listBased;

	public Transform[] toRotate;

	private void LateUpdate()
	{
		Quaternion quaternion = Quaternion.Euler(0f, 0f, ActiveCamera.Camera.rotation.Value - (float)rotation);
		if (listBased)
		{
			Transform[] array = toRotate;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].rotation = quaternion;
			}
		}
		else
		{
			base.transform.rotation = quaternion;
		}
	}
}
