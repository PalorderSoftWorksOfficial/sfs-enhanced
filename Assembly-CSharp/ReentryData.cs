using System;
using System.Collections.Generic;

[Serializable]
public struct ReentryData
{
	public string name;

	public float shockwaveM;

	public double startHeatingVelocityMultiplier;

	public double atmosphereHeight;

	public List<Point> points;
}
