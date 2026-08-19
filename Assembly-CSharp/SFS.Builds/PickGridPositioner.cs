using UnityEngine;

namespace SFS.Builds;

public class PickGridPositioner : MonoBehaviour
{
	public static PickGridPositioner main;

	public Camera cam;

	public Canvas worldSpaceCanvas;

	public Canvas screenSpaceCanvas;

	public RectTransform dropArea;

	public RectTransform pickGridRect;

	public GameObject stagingButton;

	public float targetBottom_Staging;

	private void Awake()
	{
		main = this;
	}

	private void Update()
	{
		worldSpaceCanvas.scaleFactor = screenSpaceCanvas.scaleFactor;
	}

	public Vector3 SetZ_ScreenSpaceToWorldSpace(Vector3 vec, float z)
	{
		vec = TreatUIVector(vec);
		vec = cam.ScreenToWorldPoint(vec);
		return SetZ_WorldSpace(vec, z);
	}

	public Vector3 SetZ_WorldSpace(Vector3 vec, float z)
	{
		vec = cam.WorldToScreenPoint(vec);
		return cam.ScreenToWorldPoint(new Vector3(vec.x, vec.y, z + (0f - cam.transform.position.z)));
	}

	private Vector3 TreatUIVector(Vector3 vec)
	{
		vec.z = cam.nearClipPlane;
		return vec;
	}
}
