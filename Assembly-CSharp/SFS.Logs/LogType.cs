using System;

namespace SFS.Logs;

[Serializable]
public enum LogType
{
	Null,
	Landed,
	Dock,
	Orbit,
	Changed_SOI,
	Crash,
	Atmosphere,
	Height,
	Reentry,
	LeftCapsule,
	Flag,
	CollectedRock
}
