using System;
using Newtonsoft.Json;
using SFS.Parts;

namespace SFS.World;

[Serializable]
[JsonConverter(typeof(WorldSave.LocationData.LocationConverter))]
public class RocketSave
{
	public string rocketName;

	public WorldSave.LocationData location;

	public float rotation;

	public float angularVelocity;

	public bool throttleOn;

	public float throttlePercent;

	public bool RCS;

	public PartSave[] parts;

	public JointSave[] joints;

	public StageSave[] stages = new StageSave[0];

	public bool staging_EditMode;

	public int branch = -1;

	public RocketSave()
	{
	}

	public RocketSave(Rocket rocket)
	{
		rocketName = rocket.rocketName;
		location = new WorldSave.LocationData(rocket.location.Value);
		rotation = rocket.rb2d.transform.eulerAngles.z;
		angularVelocity = rocket.rb2d.angularVelocity;
		throttleOn = rocket.throttle.throttleOn;
		throttlePercent = rocket.throttle.throttlePercent;
		RCS = rocket.arrowkeys.rcs;
		parts = PartSave.CreateSaves(rocket.partHolder.GetArray());
		joints = JointSave.CreateSave(rocket);
		stages = StageSave.CreateSaves(rocket.staging, rocket.partHolder.parts);
		staging_EditMode = rocket.staging.editMode;
		branch = rocket.stats.branch;
	}
}
