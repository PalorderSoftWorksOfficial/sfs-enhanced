using SFS.Stats;
using SFS.World;
using SFS.WorldBase;

namespace SFS.Logs;

public abstract class ChallengeStep
{
	public Planet planet;

	public bool IsEligible(Planet currentPlanet)
	{
		if (!(planet == currentPlanet))
		{
			return planet == null;
		}
		return true;
	}

	public abstract bool IsCompleted(Location location, StatsRecorder recorder, ref string progressData);

	public virtual string OnConflict(string a, string b)
	{
		return null;
	}
}
