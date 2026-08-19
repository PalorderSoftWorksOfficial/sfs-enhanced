namespace SFS.WorldBase;

public static class PlanetLoader_Static
{
	public static bool HasPlanet(this string input)
	{
		return Base.planetLoader.planets.ContainsKey(input);
	}

	public static Planet GetPlanet(this string input)
	{
		return Base.planetLoader.planets[input];
	}
}
