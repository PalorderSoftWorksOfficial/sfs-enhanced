namespace SFS.World.Drag;

public struct Surface(HeatModuleBase owner, Valid valid, Line2 line)
{
	public HeatModuleBase owner = owner;

	public Valid valid = valid;

	public Line2 line = line;
}
