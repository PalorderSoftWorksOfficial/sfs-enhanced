using SFS.Stats;
using SFS.World;

namespace SFS.Logs;

public class Step_Land : ChallengeStep
{
	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string _)
	{
		if (location.planet == planet && recorder.tracker.state_Landed)
		{
			return location.velocity.Mag_LessThan(0.1);
		}
		return false;
	}
}
