using System;
using UnityEngine;

namespace SFS.Parts.Modules;

public class OrientationModule : MonoBehaviour, I_InitializePartModule
{
	[SerializeField]
	public Orientation_Local orientation = new Orientation_Local
	{
		Value = new Orientation(1f, 1f, 0f)
	};

	int I_InitializePartModule.Priority => 11;

	void I_InitializePartModule.Initialize()
	{
		orientation.OnChange += new Action(ApplyOrientation);
	}

	public void ApplyOrientation()
	{
		base.transform.localScale = new Vector3(orientation.Value.x, orientation.Value.y, 1f);
		base.transform.localEulerAngles = new Vector3(0f, 0f, orientation.Value.z);
	}

	public static Vector2 operator *(Vector2 a, OrientationModule orientation)
	{
		return Quaternion.Euler(0f, 0f, orientation.orientation.Value.z) * new Vector2(a.x * orientation.orientation.Value.x, a.y * orientation.orientation.Value.y);
	}

	public static Vector3 operator *(Vector3 a, OrientationModule orientation)
	{
		return Quaternion.Euler(0f, 0f, orientation.orientation.Value.z) * new Vector3(a.x * orientation.orientation.Value.x, a.y * orientation.orientation.Value.y, 0f);
	}
}
