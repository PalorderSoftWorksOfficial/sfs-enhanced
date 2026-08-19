using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Input;
using SFS.Variables;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class MapManager : MonoBehaviour
{
	public GameObject mapSystemHolder;

	public TimewarpTo timewarpTo;

	public Bool_Local mapMode;

	public GameObject mapHolder;

	private void Start()
	{
		mapMode.OnChange += new Action(OnMapModeChange);
		Screen_Game map_Input = GameManager.main.map_Input;
		map_Input.onInputEnd = (Action<OnInputEndData>)Delegate.Combine(map_Input.onInputEnd, new Action<OnInputEndData>(OnInputEnd));
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Toggle_Map, ToggleMap);
	}

	private void OnMapModeChange()
	{
		mapSystemHolder.SetActive(mapMode);
		if ((bool)mapMode)
		{
			DrawMap();
		}
	}

	private void OnInputEnd(OnInputEndData data)
	{
		if (data.LeftClick)
		{
			Vector2 clickPosition = data.position.World((float)((double)Map.view.view.distance / 1000.0));
			if (TrySelect(clickPosition))
			{
				timewarpTo.Unselect();
				SoundPlayer.main.clickSound.Play();
			}
			else if (timewarpTo.TrySelect(clickPosition))
			{
				Unselect();
				SoundPlayer.main.clickSound.Play();
			}
			else
			{
				Unselect();
				timewarpTo.Unselect();
			}
		}
	}

	private static bool TrySelect(Vector2 clickPosition)
	{
		SelectableObject selectableObject = RaycastMapElements(clickPosition);
		bool num = selectableObject != null;
		if (num)
		{
			GameSelector.main.selected_Map.Value = selectableObject;
		}
		return num;
	}

	private static void Unselect()
	{
		GameSelector.main.selected_Map.Value = null;
	}

	private static SelectableObject RaycastMapElements(Vector2 clickPosition)
	{
		float clickDistanceSqrt = Mathf.Pow(Map.view.ToConstantSize(0.04f), 2f);
		return SelectableObject.mapObjects.Where((SelectableObject element) => !(element is RockSelector) && GetDistanceSqrt(element, clickPosition) < clickDistanceSqrt).GetBest((SelectableObject a, SelectableObject b) => a.ClickDepth < b.ClickDepth || (a.ClickDepth == b.ClickDepth && GetDistanceSqrt(a, clickPosition) < GetDistanceSqrt(b, clickPosition)));
		static float GetDistanceSqrt(SelectableObject A, Vector2 position)
		{
			return (position - (Vector2)A.Select_MenuPosition).sqrMagnitude;
		}
	}

	public void ToggleMap()
	{
		mapMode.Value = !mapMode;
	}

	private void LateUpdate()
	{
		DrawMap();
	}

	private void DrawMap()
	{
		DrawReset();
		Map.environment.PositionPlanets();
		Map.navigation.DrawNavigation();
		DrawPlanetDots();
		DrawTrajectories();
		DrawPlanetNames();
		DrawLandmarks();
		Map.navigation.DrawTargetIcon(Map.navigation.target != GameSelector.main.selected_Map.Value);
		timewarpTo.Draw();
	}

	private static void DrawTrajectories()
	{
		Dictionary<SelectableObject, Action> dictionary = new Dictionary<SelectableObject, Action>();
		foreach (Planet planet in Base.planetLoader.planets.Values)
		{
			if (planet.HasParent)
			{
				dictionary[planet.mapPlanet] = delegate
				{
					float alpha = (planet.data.basics.significant ? 1f : Mathf.Lerp(0.3f, 1f, MapDrawer.GetFadeOut(Map.view.view.distance, planet.Radius * 30000.0, planet.Radius * 30000.0 * 1.5)));
					planet.trajectory.DrawSolid(drawStats: false, drawStartAndEndText: false, alpha);
				};
			}
		}
		if (Map.view.view.target.Value is MapPlayer)
		{
			dictionary[Map.view.view.target.Value] = delegate
			{
				Map.view.view.target.Value.Trajectory.DrawSolid(drawStats: true, drawStartAndEndText: true);
			};
		}
		if (Map.navigation.target != null)
		{
			dictionary[Map.navigation.target] = delegate
			{
				Map.navigation.target.Trajectory.DrawSolid(drawStats: false, drawStartAndEndText: false);
			};
		}
		if (GameSelector.main.selected_Map.Value is MapPlayer)
		{
			dictionary[GameSelector.main.selected_Map.Value] = delegate
			{
				GameSelector.main.selected_Map.Value.Trajectory.DrawSolid(drawStats: true, drawStartAndEndText: true);
			};
		}
		if (PlayerController.main.player.Value != null && PlayerController.main.player.Value.mapPlayer != null)
		{
			dictionary[PlayerController.main.player.Value.mapPlayer] = delegate
			{
				PlayerController.main.player.Value.mapPlayer.Trajectory.DrawSolid(drawStats: true, drawStartAndEndText: true);
			};
		}
		foreach (Action value in dictionary.Values)
		{
			value();
		}
	}

	private static void DrawPlanetNames()
	{
		Vector2 normal = new Double2(0.0, -1.0).Rotate(GameCamerasManager.main.map_Camera.CameraRotationRadians);
		Dictionary<SelectableObject, Action> dictionary = new Dictionary<SelectableObject, Action>();
		foreach (Planet planet in Base.planetLoader.planets.Values)
		{
			dictionary[planet.mapPlanet] = delegate
			{
				DrawPlanetName(normal, planet, selected: false, target: false);
			};
		}
		SelectableObject value = GameSelector.main.selected_Map.Value;
		MapPlanet selectedPlanet = value as MapPlanet;
		if ((object)selectedPlanet != null)
		{
			dictionary[selectedPlanet] = delegate
			{
				DrawPlanetName(normal, selectedPlanet.planet, selected: true, target: false);
			};
		}
		value = Map.navigation.target;
		MapPlanet navigationPlanet = value as MapPlanet;
		if ((object)navigationPlanet != null)
		{
			dictionary[navigationPlanet] = delegate
			{
				DrawPlanetName(normal, navigationPlanet.planet, selected: false, target: true);
			};
		}
		foreach (Action value2 in dictionary.Values)
		{
			value2();
		}
	}

	private static void DrawPlanetName(Vector2 normal, Planet planet, bool selected, bool target)
	{
		if (selected)
		{
			Map.elementDrawer.DrawTextElement("    ", normal, 40, Color.white, planet.mapHolder.position, 0, clearBelow: true, 2);
			return;
		}
		float num = MapDrawer.GetFadeIn(Map.view.view.distance, planet.Radius * 200.0, planet.Radius * 200.0 * 1.3);
		int priority = ((!target) ? (planet.orbitalDepth * -100 - 100 + planet.satelliteIndex) : 0);
		if (!planet.data.basics.significant && !target)
		{
			num = Mathf.Min(MapDrawer.GetFadeOut(Map.view.view.distance, planet.Radius * 10000.0, planet.Radius * 10000.0 * 1.3), num);
		}
		Map.elementDrawer.DrawTextElement(planet.DisplayName, normal, 40, new Color(1f, 1f, 1f, num), planet.mapHolder.position, priority, clearBelow: true, 2);
	}

	private static void DrawPlanetDots()
	{
		foreach (Planet value in Base.planetLoader.planets.Values)
		{
			float num = (value.data.basics.significant ? 1f : Mathf.Lerp(0.3f, 1f, MapDrawer.GetFadeOut(Map.view.view.distance, value.Radius * 30000.0, value.Radius * 30000.0 * 1.5)));
			if (value.HasParent)
			{
				double num2 = Math.Min(value.orbit.apoapsis, value.orbit.Planet.SOI);
				num *= MapDrawer.GetFadeIn((double)Map.view.view.distance / 70.0, num2, num2 * 0.75);
			}
			if (num > 0f)
			{
				Map.elementDrawer.DrawTextElement("●", Vector2.zero, 20, value.data.basics.mapColor * new Color(1f, 1f, 1f, num), value.mapHolder.position, -1, clearBelow: false, 1);
			}
		}
	}

	private static void DrawLandmarks()
	{
		foreach (Planet value in Base.planetLoader.planets.Values)
		{
			Landmark[] landmarks = value.landmarks;
			foreach (Landmark landmark in landmarks)
			{
				double num = value.data.basics.radius * (double)(2f + landmark.data.AngularWidth);
				float num2 = Mathf.Min(MapDrawer.GetFadeIn(Map.view.view.distance, num * 0.5, num * 0.4), MapDrawer.GetFadeOut(Map.view.view.distance, 20000.0, 15000.0));
				if (!(num2 <= 0f))
				{
					Color color = new Color(1f, 1f, 1f, num2);
					Vector2 normal = Double2.CosSin((float)Math.PI / 180f * landmark.data.angle);
					Vector2 position = (Vector2)value.mapHolder.position + (Vector2)(landmark.position / 1000.0);
					MapDrawer.DrawPointWithText(15, color, landmark.displayName, 40, color, position, normal, 4, 4);
				}
			}
		}
	}

	private static void DrawReset()
	{
		Map.solidLine.pool.Reset();
		Map.dashedLine.pool.Reset();
		Map.elementDrawer.ResetElements();
	}
}
