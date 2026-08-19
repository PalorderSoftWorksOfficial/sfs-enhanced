using System;
using System.Collections.Generic;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.PlanetModules;

[Serializable]
public class BasicModule
{
	[Header("Basic data")]
	public double radius = -1.0;

	public Dictionary<Difficulty.DifficultyType, double> radiusDifficultyScale = new Dictionary<Difficulty.DifficultyType, double>();

	public double gravity = -1.0;

	public Dictionary<Difficulty.DifficultyType, double> gravityDifficultyScale = new Dictionary<Difficulty.DifficultyType, double>();

	public double timewarpHeight = -1.0;

	public double velocityArrowsHeight = double.NaN;

	[Header("Map Data")]
	public Color mapColor = Color.gray;

	public bool significant = true;

	public bool rotateCamera = true;
}
