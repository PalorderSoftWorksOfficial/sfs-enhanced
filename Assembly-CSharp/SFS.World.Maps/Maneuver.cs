using System;
using UnityEngine;

namespace SFS.World.Maps;

public class Maneuver
{
	public Orbit orbit;

	public double deltaV;

	public Action<Color> draw;

	public Maneuver(Orbit orbit, double deltaV, Action<Color> draw)
	{
		this.orbit = orbit;
		this.deltaV = deltaV;
		this.draw = draw;
	}
}
