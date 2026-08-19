using System;
using SFS.Parts.Modules;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class SpaceCenter : MonoBehaviour
{
	[Serializable]
	public class Building
	{
		public StaticWorldObject building;
	}

	public Building vab;

	public Building launchPad;

	public ModelSetup buildings;

	private void Start()
	{
		SpaceCenterData spaceCenter = Base.planetLoader.spaceCenter;
		Planet planet = spaceCenter.address.GetPlanet();
		vab.building.location.Value = new Location(planet, spaceCenter.LaunchPadLocation.position + new Double2(-300.0, -8.0));
		launchPad.building.location.Value = new Location(planet, spaceCenter.LaunchPadLocation.position);
		buildings.SetMesh();
	}
}
