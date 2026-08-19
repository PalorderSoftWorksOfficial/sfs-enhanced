using SFS.Stats;
using SFS.World;

namespace SFS.Logs;

public class Step_Orbit : ChallengeStep
{
	public StatsRecorder.Tracker.State_Orbit orbit;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string _)
	{
		if (location.planet == planet)
		{
			return recorder.tracker.state_Orbit == orbit;
		}
		return false;
	}
}
