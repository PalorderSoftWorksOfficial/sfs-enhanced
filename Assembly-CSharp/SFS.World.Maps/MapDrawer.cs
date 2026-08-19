using System;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class MapDrawer : MonoBehaviour
{
	public const int MapScale = 1000;

	public float[] alphaPerDepth = new float[20];

	public RectTransform mapTransform;

	private void Start()
	{
		Map.view.view.position.OnChange += new Action(UpdateOrbitLinesAlpha);
		Map.view.view.distance.OnChange += new Action(UpdateOrbitLinesAlpha);
	}

	private void UpdateOrbitLinesAlpha()
	{
		SelectableObject selectableObject = Map.view.view.target;
		if (selectableObject == null)
		{
			return;
		}
		for (int i = 0; i < alphaPerDepth.Length; i++)
		{
			if (selectableObject is MapPlanet mapPlanet)
			{
				alphaPerDepth[i] = GetOrbitLineAlpha(i, selectableObject.OrbitDepth - 1, mapPlanet.planet.SOI, Map.view.view.position.Value.magnitude);
			}
			else
			{
				alphaPerDepth[i] = GetOrbitLineAlpha(i, selectableObject.OrbitDepth - 2, selectableObject.Location.planet.SOI, (selectableObject.Location.position + Map.view.view.position.Value).magnitude);
			}
		}
		static float GetOrbitLineAlpha(int depth, int targetDepth, double SOI, double positionDistance)
		{
			if (depth > targetDepth)
			{
				return 1f;
			}
			if (depth < targetDepth)
			{
				return 0f;
			}
			float fadeIn = GetFadeIn(positionDistance, SOI * 2.0 * 0.75, SOI * 2.0);
			float fadeIn2 = GetFadeIn(Map.view.view.distance.Value, SOI * 6.0 * 0.75, SOI * 6.0);
			return Math.Max(fadeIn, fadeIn2);
		}
	}

	public static void DrawPointWithText(int dotSize, Color dotColor, string text, int textSize, Color textColor, Vector2 position, Vector2 normal, int priority, int renderOrder)
	{
		DrawPoint(dotSize, dotColor, position, priority, clearBelow: true, renderOrder);
		Map.elementDrawer.DrawTextElement(text, normal, textSize, textColor, position, priority, clearBelow: true, renderOrder);
	}

	public static void DrawPoint(int dotSize, Color dotColor, Vector2 position, int priority, bool clearBelow, int renderOrder)
	{
		Map.elementDrawer.DrawTextElement("●", Vector2.zero, dotSize, dotColor, position, priority, clearBelow, renderOrder);
	}

	public static float GetFadeIn(double distance, double startFadeIn, double completeFadeIn)
	{
		float num = Mathf.InverseLerp((float)startFadeIn, (float)completeFadeIn, (float)distance);
		return num * num;
	}

	public static float GetFadeOut(double distance, double startFadeOut, double completeFadeOut)
	{
		return 1f - GetFadeIn(distance, startFadeOut, completeFadeOut);
	}

	public static Vector3 GetPosition(Location a)
	{
		return GetPosition(a.planet, a.position);
	}

	public static Vector3 GetPosition(Planet planet, Double2 position)
	{
		return planet.mapHolder.position + position / 1000.0;
	}
}
