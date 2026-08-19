using SFS.WorldBase;

namespace SFS.World;

public interface I_Path
{
	Planet Planet { get; }

	PathType PathType { get; }

	double PathEndTime { get; }

	Planet NextPlanet { get; }

	Location GetLocation(double time);

	double GetStopTimewarpTime(double timeOld, double timeNew);

	bool UpdateEncounters();
}
