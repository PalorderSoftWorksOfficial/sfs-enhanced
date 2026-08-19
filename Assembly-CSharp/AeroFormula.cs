using System;
using SFS;
using SFS.World.Drag;
using UnityEngine;

[Serializable]
public struct AeroFormula
{
	public float velPow;

	public float densityPow;

	public float tempOffset;

	public float m;

	public void GetEverything(double velocity, double velocity_Y, double density, float startHeatingVelocityMultiplier, float shockwaveM, out float Q, out float shockOpacity, out float temperature, out float pure)
	{
		Q = GetQ(velocity, density);
		temperature = GetTemperature(velocity, velocity_Y, density, startHeatingVelocityMultiplier * Base.worldBase.settings.difficulty.MinHeatVelocityMultiplier * 250f);
		shockOpacity = GetShockOpacity(Q, (float)velocity, (float)density, shockwaveM, temperature, out pure);
	}

	private float GetShockOpacity(float Q, float velocity, float density, float shockwaveM, float temperature, out float pure)
	{
		float drag = Q / 50f;
		pure = Apply();
		float num = density * 2000f - 0.2f;
		float num2 = Mathf.Max((velocity - 100f) / 80f, 0f);
		float num3 = ((temperature > 0f) ? (temperature * -0.006f) : (temperature * -0.0006f));
		float[] array = new float[3] { num, num2, num3 };
		foreach (float a in array)
		{
			drag = Mathf.Min(drag, Mathf.Max(a, 0f));
		}
		return Apply() * shockwaveM;
		float Apply()
		{
			return AeroModule.GetIntensity(drag, 3f) * 2f;
		}
	}

	private float GetTemperature(double velocity, double velocity_Y, double density, float minHeatVelocity)
	{
		float heatVelocityMultiplier = Base.worldBase.settings.difficulty.HeatVelocityMultiplier;
		velocity /= (double)heatVelocityMultiplier;
		velocity_Y /= (double)heatVelocityMultiplier;
		minHeatVelocity /= heatVelocityMultiplier;
		if (velocity_Y > 0.0)
		{
			velocity -= Math.Min(velocity_Y * 2.5, velocity * 0.5);
		}
		float num = (float)(Math.Pow(velocity, velPow) * Math.Pow(density, 1f / densityPow)) / m;
		num += tempOffset * (float)(0.2 + ((velocity_Y > 0.0) ? Math.Min(velocity_Y / velocity * 2.0, 0.4) : 0.0));
		float num2 = ((float)velocity - minHeatVelocity) * 6f;
		if (num > num2)
		{
			num = num2;
		}
		if (num > 2000f)
		{
			num = 2000f + (num - 2000f) / 1.5f;
		}
		if (!(num > 0f))
		{
			return 0f;
		}
		return num;
	}

	private float GetQ(double velocity, double density)
	{
		return (float)(velocity * velocity * density);
	}
}
