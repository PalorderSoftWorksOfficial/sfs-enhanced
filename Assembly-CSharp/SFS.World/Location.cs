using System;
using SFS.WorldBase;

namespace SFS.World;

[Serializable]
public class Location
{
	public double time;

	public Planet planet;

	public Double2 position;

	public Double2 velocity;

	public double Radius => position.magnitude;

	public double Height => Radius - planet.Radius;

	public double TerrainHeight => Height - planet.GetTerrainHeightAtAngle(position.AngleRadians);

	public double VerticalVelocity => velocity.Rotate(0.0 - (position.AngleRadians - Math.PI / 2.0)).y;

	public Double2 GetSolarSystemPosition(double time)
	{
		return position + planet.GetSolarSystemPosition(time);
	}

	public Location(double time, Planet planet, Double2 position, Double2 velocity)
	{
		this.time = time;
		this.planet = planet;
		this.position = new Double2(CheckNaN(position.x), CheckNaN(position.y));
		this.velocity = new Double2(CheckNaN(velocity.x), CheckNaN(velocity.y));
	}

	public Location(Planet planet, Double2 position, Double2 velocity = default(Double2))
	{
		time = -1.0;
		this.planet = planet;
		this.position = new Double2(CheckNaN(position.x), CheckNaN(position.y));
		this.velocity = new Double2(CheckNaN(velocity.x), CheckNaN(velocity.y));
	}

	private double CheckNaN(double a)
	{
		if (!double.IsNaN(a))
		{
			return a;
		}
		return 0.0;
	}

	public static Location operator +(Location a, Location b)
	{
		return new Location(b.time, b.planet, a.position + b.position, a.velocity + b.velocity);
	}
}
