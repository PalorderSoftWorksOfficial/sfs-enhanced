using System;
using System.Collections.Generic;
using SFS.Builds;
using UnityEngine;

namespace SFS.Parts.Modules;

public class AdaptTriggerModule : MonoBehaviour, I_InitializePartModule
{
	public AdaptTriggerPoint[] points;

	[NonSerialized]
	public Dictionary<int, AdaptModule.Point> occupied = new Dictionary<int, AdaptModule.Point>();

	private bool initialized;

	int I_InitializePartModule.Priority => 0;

	void I_InitializePartModule.Initialize()
	{
		AdaptTriggerPoint[] array = points;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].owner = this;
		}
		if (BuildManager.main != null)
		{
			array = points;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled.OnChange += (Action)delegate
				{
					AdaptModule.OnCanAdaptChange(base.transform, initialized);
				};
			}
		}
		initialized = true;
	}
}
