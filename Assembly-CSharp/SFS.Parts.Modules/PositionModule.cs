using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class PositionModule : MonoBehaviour, I_InitializePartModule
{
	public Composed_Vector2 position;

	public int Priority => 8;

	public void Initialize()
	{
		position.OnChange += new Action(Position);
	}

	public void Position()
	{
		base.transform.localPosition = position.Value;
	}
}
