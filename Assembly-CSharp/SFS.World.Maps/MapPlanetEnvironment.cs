using System;
using SFS.World.Terrain;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

[Serializable]
public class MapPlanetEnvironment
{
	public Planet planet;

	public Transform holder;

	public DynamicTerrain terrain;
}
