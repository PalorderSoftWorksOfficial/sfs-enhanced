using SFS.Stats;
using SFS.World;

namespace SFS.Logs;

public class Step_Height : ChallengeStep
{
	public int height;

	public bool checkVelocity;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string _)
	{
		if (location.Height > (double)height)
		{
			if (checkVelocity)
			{
				return location.velocity.Mag_MoreThan(20.0);
			}
			return true;
		}
		return false;
	}
}
