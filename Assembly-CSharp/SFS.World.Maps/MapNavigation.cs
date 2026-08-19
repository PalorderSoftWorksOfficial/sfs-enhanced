using System;
using SFS.Cameras;
using SFS.Navigation;
using SFS.Translations;
using SFS.Variables;
using UnityEngine;

namespace SFS.World.Maps;

public class MapNavigation : MonoBehaviour
{
	public SelectableObject target;

	public Transform targetIcon;

	public (bool hasFutureWindow, Location futureWindowLocation, bool hasCurrentWindow, bool planetWindow) window;

	private void Start()
	{
		PlayerController.main.player.OnChange += new Action<Player>(OnPlayerChange);
		Map.manager.mapMode.OnChange += (Action)delegate
		{
			targetIcon.gameObject.SetActive(Map.manager.mapMode);
		};
	}

	private void OnPlayerChange(Player newPlayer)
	{
		if (target is MapPlayer mapPlayer && mapPlayer.Player == newPlayer)
		{
			SetTarget(null);
		}
	}

	public void ToggleTarget(SelectableObject A)
	{
		SetTarget(A.IsNavigationTarget() ? null : A);
	}

	public void SetTarget(SelectableObject newTarget)
	{
		if (target != null)
		{
			SelectableObject selectableObject = target;
			((I_ObservableMonoBehaviour)selectableObject).OnDestroy = (Action)Delegate.Remove(((I_ObservableMonoBehaviour)selectableObject).OnDestroy, new Action(OnTargetDestroyed));
		}
		if (newTarget != null)
		{
			((I_ObservableMonoBehaviour)newTarget).OnDestroy = (Action)Delegate.Combine(((I_ObservableMonoBehaviour)newTarget).OnDestroy, new Action(OnTargetDestroyed));
		}
		target = newTarget;
	}

	private void OnTargetDestroyed()
	{
		target = null;
	}

	public void DrawTargetIcon(bool drawTargetIcon)
	{
		if (target == null)
		{
			drawTargetIcon = false;
		}
		targetIcon.gameObject.SetActive(drawTargetIcon);
		if (drawTargetIcon)
		{
			targetIcon.position = (Vector2)ActiveCamera.main.activeCamera.Value.camera.WorldToScreenPoint(target.Select_MenuPosition);
		}
	}

	public void DrawNavigation()
	{
		window = default((bool, Location, bool, bool));
		Player value = PlayerController.main.player.Value;
		if (value == null || value.mapPlayer == null)
		{
			return;
		}
		Trajectory trajectory = value.mapPlayer.Trajectory;
		if ((target is MapRocket && (WorldView.main.ViewLocation.position - target.Location.position).Mag_LessThan(10000.0)) || target == null || !GetOrbits(trajectory, out var orbits) || !GetOrbits(target.Trajectory, out var orbits2))
		{
			return;
		}
		Orbit orbit = orbits[0];
		Orbit orbit2 = orbits2[0];
		Location location = orbit.GetLocation(WorldTime.main.worldTime);
		if (orbit.Planet == orbit2.Planet)
		{
			if (orbits.IsValidIndex(1) && orbits[1].Planet.mapPlanet == target)
			{
				orbits[0].DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, Color.white);
				DrawDeltaV(location, location);
			}
			else
			{
				Rendezvous(location, orbit, orbit2, EncounterText);
			}
		}
		else if (orbit.Planet.parentBody != null && orbit.Planet.parentBody.mapPlanet == target)
		{
			EscapeToParent_Reentry(location, orbit);
		}
		else if (orbit.Planet.parentBody != null && orbit.Planet.parentBody == orbit2.Planet && orbit.Planet.mapPlanet != target)
		{
			if (orbits.IsValidIndex(2) && orbits[2].Planet.mapPlanet == target)
			{
				orbits[0].DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, Color.white);
				orbits[1].DrawDashed(drawStats: false, drawStartText: false, drawEndText: false, Color.white);
				DrawDeltaV(location, location);
			}
			else
			{
				EscapeToEncounter(location, orbits, orbit2, EncounterText);
			}
		}
		string EncounterText()
		{
			return target.EncounterText;
		}
		static bool GetOrbits(Trajectory a, out Orbit[] reference)
		{
			reference = new Orbit[a.paths.Count];
			for (int i = 0; i < a.paths.Count; i++)
			{
				if (!(a.paths[i] is Orbit orbit3))
				{
					return false;
				}
				reference[i] = orbit3;
			}
			return reference.Length != 0;
		}
	}

	private void Rendezvous(Location location, Orbit fromOrbit, Orbit targetOrbit, Func<string> encounterText)
	{
		if (WontBlink(fromOrbit))
		{
			ManeuverTree maneuverTree = new ManeuverTree();
			Basic.Direct_Current(location, fromOrbit, targetOrbit, encounterText, target.Navigation_Tolerance, maneuverTree);
			Maneuver[] bestManeuvers = maneuverTree.GetBestManeuvers();
			bool below = targetOrbit.GetRadiusAtAngle(location.position.AngleRadians) > location.Radius;
			if (bestManeuvers.Length == 1 && ValidateTransferEfficiency(bestManeuvers[0].orbit, targetOrbit, below))
			{
				DrawDeltaV(location, bestManeuvers[0].orbit.GetLocation(bestManeuvers[0].orbit.orbitStartTime));
				bestManeuvers[0].draw(Color.white);
				window = (hasFutureWindow: false, futureWindowLocation: null, hasCurrentWindow: true, planetWindow: false);
				return;
			}
		}
		Basic.GetPredictedHohman(location, fromOrbit, targetOrbit, target.Navigation_Tolerance, out var predicted, out var departureLocation);
		if (predicted != null)
		{
			double nextAnglePassTime = fromOrbit.GetNextAnglePassTime(location.time, departureLocation.position.AngleRadians);
			Location location2 = fromOrbit.GetLocation(nextAnglePassTime);
			MapDrawer.DrawPointWithText(15, Color.white, Loc.main.Transfer, 40, Color.white, MapDrawer.GetPosition(location2), location2.position.normalized, 4, 4);
			predicted.SetEncounter(null, predicted.GetNextAnglePassTime(location.time, Intersection.GetIntersectionAngle(predicted, targetOrbit)), encounterText);
			predicted.DrawDashed(drawStats: false, drawStartText: false, drawEndText: true, Color.white);
			window = (hasFutureWindow: true, futureWindowLocation: location2, hasCurrentWindow: false, planetWindow: false);
		}
	}

	private void EscapeToParent_Reentry(Location location, Orbit fromOrbit)
	{
		Orbit targetOrbit = new Orbit(location.planet.parentBody.TimewarpRadius_Descend, 0.0, 0.0, location.planet.orbit.direction, location.planet.parentBody, PathType.Eternal, null);
		bool flag = fromOrbit.apoapsis <= fromOrbit.Planet.SOI;
		double ejectionTolerance = ((fromOrbit.apoapsis > fromOrbit.Planet.SOI) ? 18.0 : 6.0);
		if (WontBlink(fromOrbit))
		{
			Orbit[] array = Escape.Escape_Hohman_Current(location, targetOrbit, 1000.0);
			if (array != null && ValidateEjectionAngle(location, ejectionTolerance, array[0]))
			{
				DrawDeltaV(location, array[0].GetLocation(array[0].orbitStartTime));
				array[0].DrawDashed(drawStats: false, drawStartText: false, flag, Color.white);
				array[1].SetEncounter(null, array[1].GetNextTrueAnomalyPassTime(array[1].orbitStartTime, 0.0), null);
				array[1].DrawDashed(drawStats: false, flag, drawEndText: false, Color.white);
				window = (hasFutureWindow: false, futureWindowLocation: null, hasCurrentWindow: true, planetWindow: false);
				return;
			}
		}
		if (flag)
		{
			Orbit[] array2 = Escape.Escape_Hohman_Future(fromOrbit, location.time, targetOrbit, -0.10471975430846214 * (double)fromOrbit.direction, 200.0, debug: false);
			if (array2 != null)
			{
				double nextAnglePassTime = fromOrbit.GetNextAnglePassTime(location.time, array2[0].GetLocation(location.time).position.AngleRadians);
				Location location2 = fromOrbit.GetLocation(nextAnglePassTime);
				MapDrawer.DrawPointWithText(15, Color.white, Loc.main.Transfer, 40, Color.white, MapDrawer.GetPosition(location2), location2.position.normalized, 4, 4);
				array2[0].DrawDashed(drawStats: false, drawStartText: false, drawEndText: true, Color.white);
				array2[1].SetEncounter(null, array2[1].GetNextTrueAnomalyPassTime(array2[1].orbitStartTime, 0.0), null);
				array2[1].DrawDashed(drawStats: false, drawStartText: true, drawEndText: false, Color.white);
				window = (hasFutureWindow: true, futureWindowLocation: location2, hasCurrentWindow: false, planetWindow: false);
			}
		}
	}

	private void EscapeToEncounter(Location location, Orbit[] fromOrbits, Orbit targetOrbit, Func<string> encounterText)
	{
		Location location2 = fromOrbits[0].Planet.GetLocation(location.time);
		double angleRadians = location2.position.AngleRadians;
		bool below = targetOrbit.GetRadiusAtAngle(angleRadians) > location2.Radius;
		bool isNotEscape = fromOrbits[0].apoapsis <= fromOrbits[0].Planet.SOI;
		if (WontBlink(fromOrbits[0]))
		{
			ManeuverTree maneuverTree = new ManeuverTree();
			Escape.Escape_Direct_Current(location, targetOrbit, encounterText, isNotEscape, target.Navigation_Tolerance, maneuverTree);
			Maneuver[] bestManeuvers = maneuverTree.GetBestManeuvers();
			if (bestManeuvers.Length == 2 && ValidateEjectionAngle(location, 6.0, bestManeuvers[0].orbit) && ValidateTransferEfficiency(bestManeuvers[1].orbit, targetOrbit, below))
			{
				DrawDeltaV(location, bestManeuvers[0].orbit.GetLocation(bestManeuvers[0].orbit.orbitStartTime));
				bestManeuvers[0].draw(Color.white);
				bestManeuvers[1].draw(Color.white);
				window = (hasFutureWindow: false, futureWindowLocation: null, hasCurrentWindow: TryGetFutureOnCurrentPath(forTest: true), planetWindow: false);
				return;
			}
		}
		if (!TryGetFutureOnCurrentPath(forTest: false))
		{
			Orbit[] array = Escape.Escape_Hohman_Future(fromOrbits[0], location.time, targetOrbit, -0.10471975430846214 * (double)fromOrbits[0].direction, target.Navigation_Tolerance, debug: false);
			if (array != null)
			{
				Orbit orbit = fromOrbits[0].Planet.orbit;
				double angleRadians2 = array[1].GetTrueAnomaly(array[1].orbitStartTime) + array[1].arg + Basic.GetArrivalDiff(location, array[1], targetOrbit) * (double)(below ? 1 : (-1));
				Location location3 = new Location(orbit.GetNextAnglePassTime(location.time, angleRadians2), orbit.Planet, orbit.GetPositionAtAngle(angleRadians2), orbit.GetVelocityAtAngle(angleRadians2));
				Orbit hohmanTransfer = Basic.GetHohmanTransfer(location3, targetOrbit, target.Navigation_Tolerance);
				MapDrawer.DrawPointWithText(15, Color.white, Loc.main.Transfer, 40, Color.white, MapDrawer.GetPosition(location3), location3.position.normalized, 4, 4);
				hohmanTransfer.SetEncounter(null, hohmanTransfer.GetNextAnglePassTime(location.time, Intersection.GetIntersectionAngle(hohmanTransfer, targetOrbit)), encounterText);
				hohmanTransfer.DrawDashed(drawStats: false, drawStartText: false, drawEndText: true, Color.white);
				window = (hasFutureWindow: true, futureWindowLocation: location3, hasCurrentWindow: false, planetWindow: true);
			}
		}
		bool TryGetFutureOnCurrentPath(bool forTest)
		{
			ManeuverTree maneuverTree2 = new ManeuverTree();
			Escape.Escape_Direct_Future(fromOrbits[0], location.time, targetOrbit, encounterText, isNotEscape, -0.10471975430846214 * (double)fromOrbits[0].direction, target.Navigation_Tolerance, maneuverTree2);
			Maneuver[] bestManeuvers2 = maneuverTree2.GetBestManeuvers();
			if (bestManeuvers2.Length == 2 && ValidateTransferEfficiency(bestManeuvers2[1].orbit, targetOrbit, below))
			{
				if (forTest)
				{
					return true;
				}
				Location location4 = bestManeuvers2[0].orbit.GetLocation(bestManeuvers2[0].orbit.orbitStartTime);
				MapDrawer.DrawPointWithText(15, Color.white, Loc.main.Transfer, 40, Color.white, MapDrawer.GetPosition(location4), location4.position.normalized, 4, 4);
				bestManeuvers2[0].draw(Color.white);
				bestManeuvers2[1].draw(Color.white);
				location4.time = fromOrbits[0].GetNextAnglePassTime(location.time, location4.position.AngleRadians);
				window = (hasFutureWindow: true, futureWindowLocation: location4, hasCurrentWindow: false, planetWindow: false);
				return true;
			}
			return false;
		}
	}

	private static bool ValidateEjectionAngle(Location location, double ejectionTolerance, Orbit ejectionOrbit)
	{
		float num = Mathf.Abs(Mathf.DeltaAngle((float)location.planet.orbit.GetLocation(location.time).velocity.AngleDegrees, (float)ejectionOrbit.GetLocation(ejectionOrbit.orbitEndTime).velocity.AngleDegrees));
		if (!((double)num < 0.0 + ejectionTolerance))
		{
			return (double)num > 180.0 - ejectionTolerance;
		}
		return true;
	}

	private static bool ValidateTransferEfficiency(Orbit transfer, Orbit targetOrbit, bool below)
	{
		double angleRadians = (below ? (transfer.arg + Math.PI) : transfer.arg);
		double radiusAtAngle = transfer.GetRadiusAtAngle(angleRadians);
		double radiusAtAngle2 = targetOrbit.GetRadiusAtAngle(angleRadians);
		double value = radiusAtAngle - radiusAtAngle2;
		double num = transfer.apoapsis * 0.15;
		return Math.Abs(value) < num;
	}

	private static void DrawDeltaV(Location location, Location departure)
	{
		double num = departure.velocity.magnitude - location.velocity.magnitude;
		string text = ((num > 0.0) ? " +" : " ") + num.ToVelocityString(decimals: true, doubleDecimal: true) + " ";
		MapDrawer.DrawPointWithText(15, Color.white, text, 45, Color.white, MapDrawer.GetPosition(departure), departure.position.normalized, 4, 4);
	}

	private static bool WontBlink(Orbit orbit)
	{
		if (orbit.period != 0.0 && !(orbit.period / WorldTime.main.timewarpSpeed > 10.0))
		{
			return Map.manager.timewarpTo.warp != null;
		}
		return true;
	}
}
