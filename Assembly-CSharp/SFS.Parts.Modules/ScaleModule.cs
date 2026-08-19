using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ScaleModule : MonoBehaviour
{
	public Composed_Vector2 scale;

	private void Start()
	{
		scale.OnChange += new Action(Scale);
	}

	public void Scale()
	{
		base.transform.localScale = scale.Value;
	}
}
