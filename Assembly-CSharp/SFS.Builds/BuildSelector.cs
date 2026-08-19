using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class BuildSelector : Selector, I_GLDrawer
{
	public Color outlineColor;

	public float width = 0.1f;

	public string holdGridLayer;

	public string activeBuildGridLayer;

	public string inactiveBuildGridLayer;

	public string buildExampleLayer;

	public RenderSortingManager renderSortingManager;

	private List<string> GetLayers()
	{
		if (!(renderSortingManager != null))
		{
			return new List<string>();
		}
		return renderSortingManager.layers;
	}

	private void Start()
	{
		GLDrawer.Register(this);
	}

	private void OnDestroy()
	{
		GLDrawer.Unregister(this);
	}

	public List<Part> GetGroup(Part hit, out bool tookSelection)
	{
		if (selected.Contains(hit))
		{
			tookSelection = true;
			List<Part> list = selected.ToList();
			list.Remove(hit);
			list.Insert(0, hit);
			return list;
		}
		tookSelection = false;
		return new List<Part> { hit };
	}

	void I_GLDrawer.Draw()
	{
		if (!BuildManager.main.buildMenus.stagingMode.Value || !StagingDrawer.main.HasStageSelected())
		{
			foreach (Part item in selected)
			{
				string layer = (BuildManager.main.holdGrid.holdGrid.partsHolder.ContainsPart(item) ? holdGridLayer : (BuildManager.main.buildGrid.inactiveGrid.partsHolder.ContainsPart(item) ? inactiveBuildGridLayer : activeBuildGridLayer));
				float globalDepth = renderSortingManager.GetGlobalDepth(1f, layer);
				DrawOutline(new List<Part> { item }, symmetry: false, outlineColor, width, globalDepth);
			}
		}
		if (BuildManager.main.symmetryMode)
		{
			float globalDepth2 = renderSortingManager.GetGlobalDepth(1f, holdGridLayer);
			DrawOutline(BuildManager.main.holdGrid.holdGrid.partsHolder.parts, symmetry: true, new Color(1f, 1f, 1f, 0.3f), width, globalDepth2);
		}
	}

	public static void DrawOutline(List<Part> parts, bool symmetry, Color color, float width, float depth = 1f)
	{
		parts.ForEach(delegate(Part part)
		{
			DrawRegionalOutline((from x in part.GetModules<PolygonData>()
				where x.Click
				select x).ToList(), symmetry, color, width, depth);
		});
	}

	public static void DrawRegionalOutline(List<PolygonData> polygons, bool symmetry, Color color, float width, float depth = 1f)
	{
		foreach (PolygonData polygon in polygons)
		{
			Vector2[] verticesWorld = polygon.polygon.GetVerticesWorld(polygon.transform);
			if (symmetry)
			{
				float num = BuildManager.main.buildGrid.gridSize.centerX * 2f;
				for (int i = 0; i < verticesWorld.Length; i++)
				{
					verticesWorld[i] = new Vector2(0f - verticesWorld[i].x + num, verticesWorld[i].y);
				}
			}
			for (int j = 0; j < verticesWorld.Length; j++)
			{
				GLDrawer.DrawCircle(verticesWorld[j], width * 0.5f, 12, color, depth);
				GLDrawer.DrawLine(verticesWorld[j], verticesWorld[(j + 1) % verticesWorld.Length], color, width, depth);
			}
		}
	}
}
