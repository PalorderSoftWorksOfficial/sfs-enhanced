using System;
using System.Linq;
using SFS.Stats;
using SFS.World;
using SFS.World.Maps;

namespace SFS.Logs;

public class Step_Any_Landmarks : ChallengeStep
{
	public int count;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string progressData)
	{
		if (location.planet != planet || !recorder.tracker.state_Landed)
		{
			return false;
		}
		double angleDegrees = location.position.AngleDegrees;
		if (double.IsNaN(angleDegrees))
		{
			return false;
		}
		Landmark[] landmarks = planet.landmarks;
		foreach (Landmark landmark in landmarks)
		{
			if (!(Math.Abs(Math_Utility.NormalizeAngleDegrees((double)landmark.data.Center - angleDegrees)) / ((double)landmark.data.AngularWidth * 0.5) > 1.1))
			{
				if (!((progressData != null) ? progressData.Split(',', StringSplitOptions.None) : new string[0]).Contains(landmark.data.name))
				{
					progressData = (string.IsNullOrEmpty(progressData) ? "" : (progressData + ",")) + landmark.data.name;
				}
				break;
			}
		}
		if (progressData != null)
		{
			return progressData.Split(",", StringSplitOptions.None).Length >= count;
		}
		return false;
	}

	public override string OnConflict(string a, string b)
	{
		if (a == null || b == null)
		{
			return b ?? a ?? "";
		}
		if (a.Length <= b.Length)
		{
			return b;
		}
		return a;
	}
}
