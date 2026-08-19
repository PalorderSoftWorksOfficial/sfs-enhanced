using System;
using SFS.Translations;
using SFS.World.PlanetModules;

namespace SFS.WorldBase;

[Serializable]
public class Difficulty
{
	public enum DifficultyType
	{
		Normal,
		Hard,
		Realistic
	}

	public DifficultyType difficulty;

	private static readonly int[] maxPhysicsTimewarpIndex = new int[3] { 2, 2, 3 };

	private static readonly double[] defaultPlanetScales = new double[3] { 1.0, 2.0, 20.0 };

	private static readonly double[] defaultAtmosphereScales = new double[3] { 1.0, 1.6666667, 3.3333333 };

	private static readonly double[] defaultAtmosphereCurveScales = new double[3] { 1.0, 1.5, 3.0 };

	private static readonly double[] defaultDistanceScales = new double[3] { 1.0, 2.0, 20.0 };

	private static readonly double[] ispMultipliers = new double[3] { 1.0, 1.0, 1.5 };

	private static readonly double[] dryMassMultipliers = new double[3] { 1.0, 1.0, 0.25 };

	private static readonly double[] engineMassMultipliers = new double[3] { 1.0, 1.0, 0.5 };

	private static readonly int[][] altitudeMilestones = new int[3][]
	{
		new int[4] { 1000, 5000, 10000, 15000 },
		new int[4] { 1000, 10000, 20000, 30000 },
		new int[4] { 1000, 10000, 25000, 50000 }
	};

	private static readonly float[] minHeatVelocityMultiplier = new float[3] { 1f, 1.3f, 3f };

	private static readonly float[] heatVelocityMultiplier = new float[3] { 1f, 1.3f, 4.5f };

	public static Difficulty Normal => new Difficulty
	{
		difficulty = DifficultyType.Normal
	};

	public static Difficulty Hard => new Difficulty
	{
		difficulty = DifficultyType.Hard
	};

	public static Difficulty Realistic => new Difficulty
	{
		difficulty = DifficultyType.Realistic
	};

	public int MaxPhysicsTimewarpIndex => maxPhysicsTimewarpIndex[(int)difficulty];

	public double IspMultiplier => ispMultipliers[(int)difficulty];

	public double DryMassMultiplier => dryMassMultipliers[(int)difficulty];

	public double EngineMassMultiplier => engineMassMultipliers[(int)difficulty];

	public int[] AltitudeMilestones => altitudeMilestones[(int)difficulty];

	public float MinHeatVelocityMultiplier => minHeatVelocityMultiplier[(int)difficulty];

	public float HeatVelocityMultiplier => heatVelocityMultiplier[(int)difficulty];

	public double DefaultRadiusScale => defaultPlanetScales[(int)difficulty];

	public double DefaultAtmoHeightScale => defaultAtmosphereScales[(int)difficulty];

	public double DefaultAtmoCurveScale => defaultAtmosphereCurveScales[(int)difficulty];

	public double DefaultSmaScale => defaultDistanceScales[(int)difficulty];

	public string GetName()
	{
		return difficulty switch
		{
			DifficultyType.Normal => Loc.main.Difficulty_Normal, 
			DifficultyType.Hard => Loc.main.Difficulty_Hard, 
			DifficultyType.Realistic => Loc.main.Difficulty_Realistic, 
			_ => "", 
		};
	}

	public void ScalePlanetData(PlanetData planet)
	{
		double num = (planet.hasAtmospherePhysics ? AtmosphereScale(planet) : 1.0);
		BasicModule basics = planet.basics;
		basics.radius *= RadiusScale(planet);
		basics.gravity *= GravityScale(planet);
		basics.timewarpHeight *= num;
		if (planet.hasAtmospherePhysics)
		{
			Atmosphere_Physics atmospherePhysics = planet.atmospherePhysics;
			atmospherePhysics.height *= num;
			atmospherePhysics.curve *= AtmosphereCurveScale(planet);
		}
		if (planet.hasAtmosphereVisuals)
		{
			Atmosphere_Visuals atmosphereVisuals = planet.atmosphereVisuals;
			atmosphereVisuals.GRADIENT.height *= num;
			atmosphereVisuals.GRADIENT.positionZ = (int)((double)atmosphereVisuals.GRADIENT.positionZ * num);
			atmosphereVisuals.CLOUDS.startHeight *= (float)num;
			atmosphereVisuals.CLOUDS.width *= (float)num;
			atmosphereVisuals.CLOUDS.height *= (float)num;
			Atmosphere_Visuals.ColorGradient.Key[] keys = atmosphereVisuals.FOG.keys;
			for (int i = 0; i < keys.Length; i++)
			{
				keys[i].distance *= (float)num;
			}
		}
		if (planet.hasOrbit)
		{
			planet.orbit.semiMajorAxis *= SmaScale(planet);
			planet.orbit.multiplierSOI *= SoiScale(planet);
		}
	}

	public double RadiusScale(PlanetData planet)
	{
		if (!planet.basics.radiusDifficultyScale.TryGetValue(difficulty, out var value))
		{
			return DefaultRadiusScale;
		}
		return value;
	}

	public double AtmosphereScale(PlanetData planet)
	{
		if (!planet.atmospherePhysics.heightDifficultyScale.TryGetValue(difficulty, out var value))
		{
			return DefaultAtmoHeightScale;
		}
		return value;
	}

	private double GravityScale(PlanetData planet)
	{
		if (!planet.basics.gravityDifficultyScale.TryGetValue(difficulty, out var value))
		{
			return 1.0;
		}
		return value;
	}

	private double AtmosphereCurveScale(PlanetData planet)
	{
		if (!planet.atmospherePhysics.curveScale.TryGetValue(difficulty, out var value))
		{
			return DefaultAtmoCurveScale;
		}
		return value;
	}

	private double SmaScale(PlanetData planet)
	{
		if (!planet.orbit.smaDifficultyScale.TryGetValue(difficulty, out var value))
		{
			return DefaultSmaScale;
		}
		return value;
	}

	private double SoiScale(PlanetData planet)
	{
		if (!planet.orbit.soiDifficultyScale.TryGetValue(difficulty, out var value))
		{
			return 1.0;
		}
		return value;
	}
}
