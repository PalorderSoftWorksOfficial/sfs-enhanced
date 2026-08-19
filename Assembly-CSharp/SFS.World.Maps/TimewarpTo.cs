using System;
using System.Collections.Generic;
using SFS.Cameras;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World.Maps;

public class TimewarpTo : MonoBehaviour
{
	public class Select
	{
		public SelectableObject target;
	}

	public class Select_Point : Select
	{
		public Planet planet;

		public double angleRadians;
	}

	public class Select_TransferWindow : Select
	{
	}

	public interface I_Warp
	{
		SelectableObject Target { get; }
	}

	public class Warp_Point : Select_Point, I_Warp
	{
		public double startTime;

		public double endTime;

		SelectableObject I_Warp.Target => target;
	}

	public class Warp_TransferWindow : Select, I_Warp
	{
		public bool planetWindow;

		SelectableObject I_Warp.Target => target;
	}

	public Transform menuHolder;

	private Select selected;

	public I_Warp warp;

	private void Start()
	{
		menuHolder.gameObject.SetActive(value: false);
		Map.manager.mapMode.OnChange += (Action<bool>)delegate(bool mapMode)
		{
			if (!mapMode)
			{
				menuHolder.gameObject.SetActive(value: false);
			}
		};
	}

	public bool TrySelect(Vector2 clickPosition)
	{
		Select_TransferWindow select_TransferWindow = RaycastTransferWindows(clickPosition);
		if (select_TransferWindow != null)
		{
			selected = select_TransferWindow;
			return true;
		}
		Select_Point select_Point = RaycastOrbits(clickPosition);
		if (select_Point != null)
		{
			selected = select_Point;
			return true;
		}
		return false;
	}

	private static Select_Point RaycastOrbits(Vector2 clickPosition)
	{
		(SelectableObject, Planet, double, float) tuple = (null, null, 0.0, float.PositiveInfinity);
		foreach (SelectableObject target in GetTargets())
		{
			foreach (I_Path path in target.Trajectory.paths)
			{
				if (!(path is Orbit orbit) || TrajectoryDrawer.GetOrbitAlpha(orbit) == 0f)
				{
					continue;
				}
				Vector2 positionToPlanet = clickPosition - (Vector2)orbit.Planet.mapHolder.position;
				double angleRadians = Mathf.Atan2(positionToPlanet.y, positionToPlanet.x);
				if (IsInClickRange(orbit, ref angleRadians, positionToPlanet, out var distance))
				{
					double time = Math.Max(WorldTime.main.worldTime, orbit.orbitStartTime);
					double nextAnglePassTime = orbit.GetNextAnglePassTime(time, angleRadians);
					if (!(nextAnglePassTime < WorldTime.main.worldTime) && !(nextAnglePassTime > orbit.orbitEndTime) && distance < tuple.Item4)
					{
						tuple = (target, orbit.Planet, angleRadians, distance);
					}
				}
			}
		}
		if (float.IsPositiveInfinity(tuple.Item4))
		{
			return null;
		}
		Select_Point select_Point = new Select_Point();
		(select_Point.target, select_Point.planet, select_Point.angleRadians, _) = tuple;
		return select_Point;
	}

	private static List<SelectableObject> GetTargets()
	{
		List<SelectableObject> list = new List<SelectableObject>();
		if (PlayerController.main.player.Value != null && PlayerController.main.player.Value.mapPlayer != null)
		{
			list.Add(PlayerController.main.player.Value.mapPlayer);
		}
		return list;
	}

	private static bool IsInClickRange(Orbit orbit, ref double angleRadians, Vector2 positionToPlanet, out float distance)
	{
		if (orbit.GetRadiusAtAngle(angleRadians) < 0.0)
		{
			distance = float.PositiveInfinity;
			return false;
		}
		Vector2 vector = orbit.GetPositionAtAngle(angleRadians) / 1000.0;
		Vector2 vector2 = vector + orbit.GetVelocityAtAngle(angleRadians).normalized;
		float closestPointOnLine = Math_Utility.GetClosestPointOnLine(vector, vector2, positionToPlanet);
		Vector2 vector3 = Vector2.LerpUnclamped(vector, vector2, closestPointOnLine);
		distance = (vector3 - positionToPlanet).magnitude;
		if (distance > Map.view.ToConstantSize(0.03f))
		{
			return false;
		}
		angleRadians = Mathf.Atan2(vector3.y, vector3.x);
		return orbit.GetRadiusAtAngle(angleRadians) > 0.0;
	}

	private static Select_TransferWindow RaycastTransferWindows(Vector2 clickPosition)
	{
		var (flag, location, _, _) = Map.navigation.window;
		if (!flag)
		{
			return null;
		}
		MapPlayer mapPlayer = ((PlayerController.main.player.Value != null) ? PlayerController.main.player.Value.mapPlayer : null);
		if (mapPlayer == null)
		{
			return null;
		}
		Vector2 vector = location.planet.mapHolder.position + location.position / 1000.0;
		if ((clickPosition - vector).magnitude > Map.view.ToConstantSize(0.04f))
		{
			return null;
		}
		return new Select_TransferWindow
		{
			target = mapPlayer
		};
	}

	public void Unselect()
	{
		selected = null;
	}

	public void Draw()
	{
		Select obj = selected;
		bool flag2;
		Vector3 position;
		if (!(obj is Select_Point a))
		{
			if (obj is Select_TransferWindow)
			{
				var (flag, location, _, _) = Map.navigation.window;
				if (!flag)
				{
					flag2 = false;
					position = default(Vector3);
				}
				else
				{
					flag2 = true;
					position = location.planet.mapHolder.position + location.position / 1000.0;
				}
			}
			else
			{
				flag2 = false;
				position = default(Vector3);
			}
		}
		else
		{
			flag2 = GetPosition(a, out position);
		}
		if (menuHolder.gameObject.activeSelf != flag2)
		{
			menuHolder.gameObject.SetActive(flag2);
		}
		if (flag2)
		{
			MapDrawer.DrawPoint(25, Color.white, position, 5, clearBelow: true, 5);
			menuHolder.position = (Vector2)ActiveCamera.main.activeCamera.Value.camera.WorldToScreenPoint(position);
		}
		if (warp is Warp_Point a2 && GetPosition(a2, out var position2))
		{
			MapDrawer.DrawPoint(20, Color.white, position2, 5, clearBelow: true, 5);
		}
	}

	private static bool GetPosition(Select_Point a, out Vector3 position)
	{
		if (a != null)
		{
			foreach (I_Path path in a.target.Trajectory.paths)
			{
				if (path is Orbit orbit && orbit.Planet == a.planet && TryGetRadiusFromAngle(orbit, a.angleRadians, out var radius) && (double)(radius * 1000f) > a.planet.Radius + a.planet.GetTerrainHeightAtAngle(a.angleRadians))
				{
					position = orbit.Planet.mapHolder.position + Double2.CosSin(a.angleRadians) * radius;
					return true;
				}
			}
		}
		position = default(Vector3);
		return false;
	}

	private static bool TryGetRadiusFromAngle(Orbit orbit, double angleRadians, out float radius)
	{
		radius = (float)orbit.GetRadiusAtAngle(angleRadians) / 1000f;
		return radius > 0f;
	}

	public void StartTimewarp()
	{
		if (!WorldTime.CanTimewarp(showMsg: true, showSpeed: false))
		{
			return;
		}
		Select obj = selected;
		Select_Point select_Point = obj as Select_Point;
		if (select_Point == null)
		{
			if (obj is Select_TransferWindow select_TransferWindow)
			{
				var (flag, location, _, planetWindow) = Map.navigation.window;
				if (flag)
				{
					double time = location.time;
					Warp_TransferWindow warp_TransferWindow = new Warp_TransferWindow
					{
						target = select_TransferWindow.target,
						planetWindow = planetWindow
					};
					StartWarp(time - WorldTime.main.worldTime, warp_TransferWindow);
					Unselect();
				}
			}
			return;
		}
		double num = GetEndTime();
		if (!double.IsNaN(num))
		{
			if (select_Point.target is MapPlayer mapPlayer && mapPlayer.Player.isPlayer.Value && Map.view.view.target.Value != select_Point.target)
			{
				Map.view.FocusForTimewarpTo(select_Point.target);
			}
			Warp_Point warp_Point = new Warp_Point
			{
				target = select_Point.target,
				planet = select_Point.planet,
				angleRadians = select_Point.angleRadians,
				startTime = WorldTime.main.worldTime,
				endTime = num
			};
			StartWarp(warp_Point.endTime - warp_Point.startTime, warp_Point);
			Unselect();
		}
		double GetEndTime()
		{
			foreach (I_Path path in select_Point.target.Trajectory.paths)
			{
				if (path is Orbit orbit && orbit.Planet == select_Point.planet)
				{
					double time2 = Math.Max(WorldTime.main.worldTime, orbit.orbitStartTime);
					return orbit.GetNextAnglePassTime(time2, select_Point.angleRadians);
				}
			}
			return double.NaN;
		}
		void StartWarp(double warpTime, I_Warp warp)
		{
			if (warpTime < 1.0)
			{
				Debug.LogError("Invalid warp time");
			}
			else
			{
				WorldTime.main.SetState(Math.Clamp(warpTime / (2.0 + Math.Log10(warpTime)), 2.0, WorldTime.MaxTimewarpSpeed), realtimePhysics: false, showMsg: false);
				this.warp = warp;
			}
		}
	}

	public void OnStopTimewarp()
	{
		if (warp != null)
		{
			if (Map.view.view.target.Value == warp.Target)
			{
				Map.view.ToggleFocus(warp.Target, 1f);
			}
			warp = null;
		}
	}
}
