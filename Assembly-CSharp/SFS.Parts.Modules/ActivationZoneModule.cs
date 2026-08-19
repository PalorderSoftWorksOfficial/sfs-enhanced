using System;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ActivationZoneModule : MonoBehaviour
{
	[Serializable]
	public class Zone
	{
		public PolygonData[] polygons;

		public UsePartUnityEvent onPartUsed;
	}

	public Zone[] activationZones;

	public UsePartUnityEvent onPartUsed_Default;

	public bool overwriteOnStage;

	public UsePartUnityEvent onPartUsed_Staging;

	public void OnPartUsed(UsePartData data)
	{
		if (overwriteOnStage && data.sharedData.fromStaging)
		{
			onPartUsed_Staging.Invoke(data);
			return;
		}
		Zone[] array = activationZones;
		foreach (Zone zone in array)
		{
			PolygonData[] polygons = zone.polygons;
			for (int j = 0; j < polygons.Length; j++)
			{
				if (polygons[j] == data.clickPolygon)
				{
					zone.onPartUsed.Invoke(data);
					return;
				}
			}
		}
		onPartUsed_Default.Invoke(data);
	}
}
