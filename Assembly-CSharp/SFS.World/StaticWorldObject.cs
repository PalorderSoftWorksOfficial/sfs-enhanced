using System;
using UnityEngine;

namespace SFS.World;

public class StaticWorldObject : MonoBehaviour
{
	public WorldLocation location;

	private WorldLoader loader;

	private void Start()
	{
		loader = GetComponent<WorldLoader>();
		location.position.OnChange += new Action(Position);
		loader.onLoadedChange_After += delegate
		{
			Position();
		};
		WorldView.main.positionOffset.OnChange += new Action(Position);
	}

	private void OnDestroy()
	{
		WorldView.main.positionOffset.OnChange -= new Action(Position);
	}

	private void Position()
	{
		if (location != null && (loader == null || loader.Loaded))
		{
			base.transform.position = WorldView.ToLocalPosition(location.position);
		}
		else
		{
			base.transform.position = Vector3.zero;
		}
	}
}
