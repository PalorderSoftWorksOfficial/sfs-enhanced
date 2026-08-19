using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class AreaSelect : MonoBehaviour, I_GLDrawer
{
	public float width = 0.1f;

	[Space]
	public string layer;

	public RenderSortingManager renderSortingManager;

	private Vector2 start;

	private Vector2 end;

	public bool Selecting { get; private set; }

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
		GLDrawer.Register(this);
	}

	void I_GLDrawer.Draw()
	{
		if (Selecting)
		{
			Color color = (BuildManager.main.buildMenus.IsEditingStage ? StagingDrawer.main.outlineColor : BuildManager.main.buildMenus.selector.outlineColor);
			float globalDepth = renderSortingManager.GetGlobalDepth(1f, layer);
			Vector2[] array = new Vector2[4]
			{
				start,
				new Vector2(start.x, end.y),
				end,
				new Vector2(end.x, start.y)
			};
			for (int i = 0; i < array.Length; i++)
			{
				GLDrawer.DrawCircle(array[i], width * 0.5f, 12, color, globalDepth);
				GLDrawer.DrawLine(array[i], array[(i + 1) % array.Length], color, width, globalDepth);
			}
		}
	}

	public void StartSelect(Vector2 position)
	{
		Selecting = true;
		start = position;
		end = position;
	}

	public void Drag(Vector2 offset)
	{
		end += offset;
	}

	public void EndSelect()
	{
		Bounds selectArea = new Bounds((start + end) * 0.5f, new Vector2(Mathf.Abs(start.x - end.x), Mathf.Abs(start.y - end.y)));
		Select(BuildManager.main.buildGrid.activeGrid.partsHolder.parts);
		Select(BuildManager.main.buildGrid.inactiveGrid.partsHolder.parts);
		Selecting = false;
		if (Mathf.Abs(selectArea.size.x * selectArea.size.y) > 4f)
		{
			PopupManager.MarkUsed(PopupManager.Feature.AreaSelect);
		}
		void Select(List<Part> parts)
		{
			List<Part> list = new List<Part>();
			foreach (Part part in parts)
			{
				if ((from poly in part.GetModules<PolygonData>()
					where poly.Click
					select poly).All((PolygonData poly) => Part_Utility.GetBounds_WorldSpace(out var bounds, new PolygonData[1] { poly }) && selectArea.Contains(bounds.min) && selectArea.Contains(bounds.max)))
				{
					list.Add(part);
				}
			}
			BuildManager.main.buildMenus.OnAreaSelect(list.ToArray());
		}
	}

	public void CancelSelect()
	{
		Selecting = false;
	}
}
