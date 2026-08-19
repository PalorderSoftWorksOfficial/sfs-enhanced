using System;
using SFS.Stats;
using SFS.World;
using UnityEngine;

namespace SFS.Logs;

public class Step_Downrange : ChallengeStep
{
	public int downrange;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string _)
	{
		if (!recorder.tracker.state_Landed)
		{
			return false;
		}
		return Mathf.Abs(Mathf.DeltaAngle((float)Base.planetLoader.spaceCenter.angle, (float)location.position.AngleDegrees)) * ((float)Math.PI / 180f) * (float)location.planet.Radius > (float)downrange;
	}
}
