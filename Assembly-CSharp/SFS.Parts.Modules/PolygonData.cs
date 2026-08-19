using System;
using System.Collections.Generic;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public abstract class PolygonData : SurfaceData
{
	[SerializeField]
	private bool colliderArea = true;

	[SerializeField]
	private bool attachByOverlap = true;

	[SerializeField]
	private bool physicsCollider;

	[SerializeField]
	private bool clickArea = true;

	[SerializeField]
	private float baseDepth;

	[SerializeField]
	private Composed_Float composedBaseDepth;

	[SerializeField]
	private bool isComposedDepth;

	public Polygon polygon;

	public Polygon polygonFast;

	public bool Click
	{
		get
		{
			if (clickArea)
			{
				return base.isActiveAndEnabled;
			}
			return false;
		}
	}

	public bool BuildCollider
	{
		get
		{
			if (colliderArea)
			{
				return base.isActiveAndEnabled;
			}
			return false;
		}
	}

	public bool BuildCollider_IncludeInactive => colliderArea;

	public bool PhysicsCollider_IncludeInactive => physicsCollider;

	public bool AttachByOverlap => attachByOverlap;

	public float BaseDepth
	{
		get
		{
			if (!isComposedDepth)
			{
				return baseDepth;
			}
			return composedBaseDepth.Value;
		}
	}

	private void Reset()
	{
		physicsCollider = true;
	}

	private void Awake()
	{
		if (physicsCollider && GameManager.main != null)
		{
			base.gameObject.AddComponent<PolygonCollider>().polygon = this;
		}
	}

	public void SubscribeToComposedDepth(Action a)
	{
		if (isComposedDepth)
		{
			composedBaseDepth.OnChange += a;
		}
	}

	protected void SetData(Polygon polygon, Polygon polygonFast)
	{
		this.polygon = polygon;
		this.polygonFast = polygonFast;
		SetData(new List<Surfaces>
		{
			new Surfaces(polygon.vertices, loop: true, base.transform)
		}, new List<Surfaces>
		{
			new Surfaces(polygonFast.vertices, loop: true, base.transform)
		});
	}

	protected void SetData(Polygon polygon)
	{
		this.polygon = polygon;
		polygonFast = polygon;
		List<Surfaces> list = new List<Surfaces>
		{
			new Surfaces(polygon.vertices, loop: true, base.transform)
		};
		SetData(list, list);
	}

	public virtual bool Raycast(Vector2 point, out float depth)
	{
		depth = BaseDepth;
		return Polygon.GetDistanceToPolygons(point, polygon.convexPolygons) < 0f;
	}
}
