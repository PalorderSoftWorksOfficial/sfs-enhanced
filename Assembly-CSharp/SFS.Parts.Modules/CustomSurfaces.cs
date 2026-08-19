using System.Collections.Generic;
using System.Linq;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public sealed class CustomSurfaces : SurfaceData, I_InitializePartModule
{
	[HideInInspector]
	[SerializeField]
	private Composed_Vector2[] points;

	[HideInInspector]
	[SerializeField]
	private bool loop;

	[Space]
	public List<ComposedSurfaces> pointsArray = new List<ComposedSurfaces>
	{
		new ComposedSurfaces()
	};

	public bool edit;

	public bool view;

	public float gridSize = 0.1f;

	int I_InitializePartModule.Priority => 10;

	private void OnValidate()
	{
		if (points != null && points.Length != 0)
		{
			pointsArray = new List<ComposedSurfaces>
			{
				new ComposedSurfaces
				{
					points = points,
					loop = loop
				}
			};
			points = null;
		}
	}

	void I_InitializePartModule.Initialize()
	{
		Output();
	}

	public override void Output()
	{
		List<Surfaces> list = pointsArray.Select((ComposedSurfaces a) => new Surfaces(a.points.Select((Composed_Vector2 p) => p.Value).ToArray(), a.loop, base.transform)).ToList();
		SetData(list, list);
	}
}
