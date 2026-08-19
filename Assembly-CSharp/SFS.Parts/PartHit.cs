using SFS.Parts.Modules;

namespace SFS.Parts;

public class PartHit
{
	public Part part;

	public PolygonData polygon;

	public PartHit(Part part, PolygonData polygon)
	{
		this.part = part;
		this.polygon = polygon;
	}
}
