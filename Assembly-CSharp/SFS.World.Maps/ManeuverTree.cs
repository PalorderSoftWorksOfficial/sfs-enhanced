using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.World.Maps;

public class ManeuverTree
{
	private Maneuver maneuver;

	private ManeuverTree parent;

	private List<ManeuverTree> children = new List<ManeuverTree>();

	public ManeuverTree AddManeuver(Orbit orbit, double deltaV, Action<Color> draw)
	{
		ManeuverTree maneuverTree = new ManeuverTree
		{
			maneuver = new Maneuver(orbit, deltaV, draw),
			parent = this
		};
		children.Add(maneuverTree);
		return maneuverTree;
	}

	public Maneuver[] GetBestManeuvers()
	{
		List<Maneuver> list = new List<Maneuver>();
		ManeuverTree bestBranch = GetBestBranch(out var _);
		while (bestBranch != null && bestBranch.maneuver != null)
		{
			list.Add(bestBranch.maneuver);
			bestBranch = bestBranch.parent;
		}
		list.Reverse();
		return list.ToArray();
	}

	private ManeuverTree GetBestBranch(out double deltaV)
	{
		if (children.Count == 0)
		{
			if (maneuver == null)
			{
				deltaV = double.NaN;
				return null;
			}
			deltaV = maneuver.deltaV;
			return this;
		}
		ManeuverTree maneuverTree = null;
		deltaV = double.PositiveInfinity;
		foreach (ManeuverTree child in children)
		{
			ManeuverTree bestBranch = child.GetBestBranch(out var deltaV2);
			if (maneuverTree == null || deltaV2 < deltaV)
			{
				deltaV = deltaV2;
				maneuverTree = bestBranch;
			}
		}
		if (maneuver != null)
		{
			deltaV += maneuver.deltaV;
		}
		return maneuverTree;
	}

	public void DrawAll(List<Maneuver> exclude, Color color)
	{
		if (!exclude.Contains(maneuver) && maneuver != null)
		{
			maneuver.draw(color);
		}
		foreach (ManeuverTree child in children)
		{
			child.DrawAll(exclude, color);
		}
	}
}
