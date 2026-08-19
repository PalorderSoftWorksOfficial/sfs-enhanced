using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public class SurfaceCollider : ColliderModule, I_InitializePartModule
{
	public SurfaceData surfaces;

	int I_InitializePartModule.Priority => -1;

	void I_InitializePartModule.Initialize()
	{
		surfaces.onChange += new Action(CreateSurfaceColliders);
	}

	private void CreateSurfaceColliders()
	{
		List<Surfaces> list = surfaces.surfaces;
		List<EdgeCollider2D> orAddComponents = this.GetOrAddComponents<EdgeCollider2D>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			if (i < list.Count)
			{
				orAddComponents[i].points = list[i].points;
			}
			else
			{
				UnityEngine.Object.Destroy(orAddComponents[i]);
			}
		}
	}
}
