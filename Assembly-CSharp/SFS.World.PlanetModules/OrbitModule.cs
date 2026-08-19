using System;
using System.Collections.Generic;
using SFS.WorldBase;

namespace SFS.World.PlanetModules;

[Serializable]
public class OrbitModule
{
	public string parent;

	public double semiMajorAxis;

	public Dictionary<Difficulty.DifficultyType, double> smaDifficultyScale = new Dictionary<Difficulty.DifficultyType, double>();

	public double eccentricity;

	public double argumentOfPeriapsis;

	public int direction = 1;

	public double multiplierSOI = 1.0;

	public Dictionary<Difficulty.DifficultyType, double> soiDifficultyScale = new Dictionary<Difficulty.DifficultyType, double>();
}
