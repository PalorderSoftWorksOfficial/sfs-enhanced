using System;
using System.Collections.Generic;
using SFS.Variables;
using SFS.World;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.Parts.Modules;

public abstract class SurfaceData : MonoBehaviour
{
	[SerializeField]
	private bool attachmentSurfaces = true;

	[SerializeField]
	private bool dragSurfaces = true;

	public List<Surfaces> surfaces;

	public List<Surfaces> surfacesFast;

	[NonSerialized]
	public Event_Local onChange = new Event_Local();

	[NonSerialized]
	public HeatModuleBase heatModule;

	public bool Attachment
	{
		get
		{
			if (attachmentSurfaces)
			{
				return base.isActiveAndEnabled;
			}
			return false;
		}
	}

	public bool Drag
	{
		get
		{
			if (dragSurfaces)
			{
				return base.isActiveAndEnabled;
			}
			return false;
		}
	}

	public abstract void Output();

	protected void SetData(List<Surfaces> surfaces)
	{
		SetData(surfaces, surfaces);
	}

	protected void SetData(List<Surfaces> surfaces, List<Surfaces> surfacesFast)
	{
		this.surfaces = surfaces;
		this.surfacesFast = surfacesFast;
		onChange.Invoke();
	}

	public static bool IsSurfaceCovered(SurfaceData surface)
	{
		Part componentInParentTree = surface.transform.GetComponentInParentTree<Part>();
		Line2[] surfacesWorld = surface.surfaces[0].GetSurfacesWorld();
		foreach (PartJoint connectedJoint in surface.transform.GetComponentInParentTree<Rocket>().jointsGroup.GetConnectedJoints(componentInParentTree))
		{
			if (SurfaceUtility.SurfacesConnect(connectedJoint.GetOtherPart(componentInParentTree), surfacesWorld, out var _, out var _))
			{
				return true;
			}
		}
		return false;
	}

	protected void Start()
	{
		heatModule = base.transform.GetComponentInParentTree<HeatModuleBase>();
	}
}
