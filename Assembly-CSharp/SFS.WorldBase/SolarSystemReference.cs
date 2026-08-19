using System;

namespace SFS.WorldBase;

[Serializable]
public class SolarSystemReference
{
	public string name;

	public SolarSystemReference(string name)
	{
		this.name = name;
	}
}
