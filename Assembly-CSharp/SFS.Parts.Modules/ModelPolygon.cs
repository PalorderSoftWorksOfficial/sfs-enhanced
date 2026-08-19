using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ModelPolygon : PolygonData, I_InitializePartModule
{
	[HideInInspector]
	public MeshFilter[] meshes = Array.Empty<MeshFilter>();

	public List<Vector2> points;

	public bool edit;

	public bool view;

	public int Priority => 15;

	private void GetMesh()
	{
		meshes = new MeshFilter[1] { GetComponent<MeshFilter>() };
	}

	private void GetAllMeshes()
	{
		meshes = base.transform.GetComponentsInChildren<MeshFilter>();
	}

	private void GetAllMeshesRoot()
	{
		meshes = base.transform.root.GetComponentsInChildren<MeshFilter>();
	}

	private void ClearPoints()
	{
		points.Clear();
	}

	public void Initialize()
	{
		Output();
	}

	public override void Output()
	{
		SetData(new Polygon(points.ToArray()));
	}
}
