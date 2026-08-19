using System;
using System.Collections.Generic;
using SFS.WorldBase;

namespace SFS.World.PlanetModules;

[Serializable]
public class Atmosphere_Physics
{
	public double height = -1.0;

	public double density = -1.0;

	public double curve = -1.0;

	public Dictionary<Difficulty.DifficultyType, double> curveScale = new Dictionary<Difficulty.DifficultyType, double>();

	public double parachuteMultiplier = 1.0;

	public double upperAtmosphere = 0.5;

	public Dictionary<Difficulty.DifficultyType, double> heightDifficultyScale = new Dictionary<Difficulty.DifficultyType, double>();

	public float shockwaveIntensity = 0.5f;

	public float minHeatingVelocityMultiplier = 1f;
}
