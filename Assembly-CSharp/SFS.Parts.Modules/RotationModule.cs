using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class RotationModule : MonoBehaviour
{
	public Composed_Float rotation;

	private void Start()
	{
		rotation.OnChange += new Action(Rotate);
	}

	public void Rotate()
	{
		base.transform.localEulerAngles = new Vector3(0f, 0f, rotation.Value);
	}
}
