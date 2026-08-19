using System;
using SFS.Parts.Modules;

namespace SFS.Parts;

public class UsePartData
{
	public class SharedData
	{
		public readonly bool fromStaging;

		public Action onPostPartsActivation;

		public bool hasToggledRCS;

		public SharedData(bool fromStaging)
		{
			this.fromStaging = fromStaging;
		}
	}

	public SharedData sharedData;

	public bool successfullyUsedPart = true;

	public PolygonData clickPolygon;

	public UsePartData(SharedData sharedData, PolygonData clickPolygon)
	{
		this.sharedData = sharedData;
		this.clickPolygon = clickPolygon;
	}
}
