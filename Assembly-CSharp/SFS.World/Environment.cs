using System;
using SFS.World.Terrain;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

[Serializable]
public class Environment
{
	public Planet planet;

	public Transform holder;

	public DynamicTerrain terrain;

	public Atmosphere atmosphere;
}
