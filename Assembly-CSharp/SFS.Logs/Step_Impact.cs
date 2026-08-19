using SFS.Stats;
using SFS.World;

namespace SFS.Logs;

public class Step_Impact : ChallengeStep
{
	public int impactVelocity;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string _)
	{
		return false;
	}
}
