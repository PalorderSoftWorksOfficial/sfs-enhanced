using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using SFS.Logs;
using SFS.World.Maps;
using SFS.World.PlanetModules;

namespace SFS.WorldBase;

[Serializable]
public class PlanetData
{
	public string version;

	[JsonProperty("BASE_DATA")]
	public BasicModule basics;

	[JsonIgnore]
	public bool hasAtmospherePhysics;

	[JsonProperty("ATMOSPHERE_PHYSICS_DATA")]
	public Atmosphere_Physics atmospherePhysics;

	[JsonIgnore]
	public bool hasAtmosphereVisuals;

	[JsonProperty("ATMOSPHERE_VISUALS_DATA")]
	public Atmosphere_Visuals atmosphereVisuals;

	[JsonIgnore]
	public bool hasTerrain;

	[JsonProperty("TERRAIN_DATA")]
	public TerrainModule terrain;

	[JsonIgnore]
	public bool hasPostProcessing;

	[JsonProperty("POST_PROCESSING")]
	public PostProcessingModule postProcessing;

	[JsonIgnore]
	public bool hasOrbit;

	[JsonProperty("ORBIT_DATA")]
	public OrbitModule orbit;

	[JsonProperty("ACHIEVEMENT_DATA")]
	public LogsModule logs;

	[JsonProperty("LANDMARKS")]
	public List<LandmarkData> landmarks;

	[OnDeserialized]
	private void OnDeserializedMethod(StreamingContext context)
	{
		hasAtmospherePhysics = atmospherePhysics != null;
		hasAtmosphereVisuals = atmosphereVisuals != null;
		hasTerrain = terrain != null;
		hasPostProcessing = postProcessing != null;
		hasOrbit = orbit != null;
		if (logs == null)
		{
			logs = new LogsModule();
		}
		if (landmarks == null)
		{
			landmarks = new List<LandmarkData>();
		}
	}
}
