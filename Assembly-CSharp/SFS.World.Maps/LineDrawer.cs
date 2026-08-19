using System;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class LineDrawer : MonoBehaviour
{
	public Transform linePrefab;

	public float lineWidth = 0.004f;

	public float tileSize = 1f;

	public LineTextureMode lineTextureMode;

	public Pool<LineRenderer> pool;

	private Material material;

	private void Start()
	{
		material = linePrefab.GetComponent<LineRenderer>().sharedMaterial;
		pool = new Pool<LineRenderer>(CreateNew, Reset);
		Map.view.view.distance.OnChange += new Action(UpdateWidth);
	}

	private void UpdateWidth()
	{
		foreach (LineRenderer item in pool.Items)
		{
			item.widthMultiplier = Map.view.ToConstantSize(lineWidth);
		}
		material.mainTextureScale = new Vector2(1f / Map.view.ToConstantSize(tileSize), 1f);
	}

	public void DrawLine(Vector3[] points, Planet planet, Color startColor, Color endColor)
	{
		LineRenderer item = pool.GetItem();
		if (item.positionCount != points.Length)
		{
			item.positionCount = points.Length;
		}
		item.SetPositions(points);
		item.transform.parent = planet.mapHolder;
		item.transform.localPosition = Vector3.zero;
		item.startColor = startColor;
		item.endColor = endColor;
		item.gameObject.SetActive(value: true);
	}

	public LineRenderer CreateNew()
	{
		LineRenderer component = UnityEngine.Object.Instantiate(linePrefab, Vector3.zero, Quaternion.identity, Map.manager.transform).GetComponent<LineRenderer>();
		component.sortingOrder = 0;
		component.sortingLayerName = "Map";
		component.transform.gameObject.SetActive(value: false);
		component.transform.name = "Orbit Line";
		component.widthMultiplier = Map.view.ToConstantSize(lineWidth);
		component.textureMode = lineTextureMode;
		return component;
	}

	public void Reset(LineRenderer toReset)
	{
		if (toReset.gameObject.activeSelf)
		{
			toReset.gameObject.SetActive(value: false);
		}
	}
}
