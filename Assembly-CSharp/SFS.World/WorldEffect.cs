using System;
using UnityEngine;

namespace SFS.World;

public class WorldEffect : MonoBehaviour
{
	private Double2 position;

	private void Start()
	{
		position = WorldView.ToGlobalPosition(base.transform.position);
		WorldView.main.positionOffset.OnChange += new Action(Position);
	}

	private void OnDestroy()
	{
		WorldView.main.positionOffset.OnChange -= new Action(Position);
	}

	private void Position()
	{
		base.transform.position = WorldView.ToLocalPosition(position);
	}
}
