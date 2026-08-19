using System;
using System.Linq;
using SFS.Cameras;
using SFS.Translations;
using UnityEngine;

namespace SFS.World.Maps;

public static class TrajectoryDrawer
{
	public static void DrawSolid(this Trajectory trajectory, bool drawStats, bool drawStartAndEndText, float alpha = 1f)
	{
		foreach (I_Path path in trajectory.paths)
		{
			if (path is Orbit orbit)
			{
				orbit.DrawOrbit(new Color(1f, 1f, 1f, alpha), drawStats, drawStartAndEndText, drawStartAndEndText, Map.solidLine);
			}
		}
	}

	public static void DrawDashed(this Orbit orbit, bool drawStats, bool drawStartText, bool drawEndText, Color color)
	{
		orbit.DrawOrbit(color, drawStats, drawStartText, drawEndText, Map.dashedLine);
	}

	private static void DrawOrbit(this Orbit orbit, Color c, bool drawStats, bool drawStartText, bool drawEndText, LineDrawer lineDrawer)
	{
		c.a *= GetOrbitAlpha(orbit);
		if (c.a == 0f)
		{
			return;
		}
		bool flag2;
		bool flag3;
		switch (orbit.pathType)
		{
		case PathType.Eternal:
			if (orbit.PathStartTime > WorldTime.main.worldTime)
			{
				double trueAnomaly3 = orbit.GetTrueAnomaly(orbit.PathStartTime);
				double endTrueAnomaly3 = trueAnomaly3;
				string startText3 = (drawEndText ? ((string)Loc.main.Escape) : null);
				DrawOrbit(orbit, trueAnomaly3, endTrueAnomaly3, startText3, null, c, 0.25f, 0f, lineDrawer);
			}
			else
			{
				double trueAnomaly4 = orbit.GetTrueAnomaly(WorldTime.main.worldTime);
				double endTrueAnomaly4 = trueAnomaly4;
				DrawOrbit(orbit, trueAnomaly4, endTrueAnomaly4, null, null, c, 0.1f, 0.25f, lineDrawer);
			}
			flag2 = true;
			flag3 = true;
			break;
		case PathType.Escape:
		{
			bool flag4 = orbit.Planet.SOI != double.PositiveInfinity;
			double num2 = (flag4 ? orbit.Planet.SOI : ((Map.view.view.target.Value.Location.GetSolarSystemPosition(WorldTime.main.worldTime) + Map.view.view.position.Value).magnitude + Map.view.view.distance.Value * (double)ActiveCamera.Camera.ViewSizeNormal * 1.0499999523162842));
			if (num2 < orbit.periapsis)
			{
				return;
			}
			double num3 = Math.Max(WorldTime.main.worldTime, orbit.PathStartTime);
			double trueAnomaly2 = orbit.GetTrueAnomaly(num3);
			double endTrueAnomaly2 = orbit.GetTrueAnomalyAtRadius(num2) * (double)orbit.direction;
			bool flag5 = LanguageSettings.main.settings.name == "" || LanguageSettings.main.settings.name == "English";
			string startText2 = ((!drawStartText || !(num3 > WorldTime.main.worldTime)) ? null : ((!(Kepler.GetRadiusAtTrueAnomaly(orbit.slr, orbit.ecc, trueAnomaly2) < orbit.Planet.SOI * 0.9999)) ? (flag5 ? "Encounter" : ((string)Loc.main.Encounter)) : (flag5 ? "Escape" : ((string)Loc.main.Escape))));
			string endText2 = ((!(drawEndText & flag4)) ? null : (flag5 ? "Escape" : ((string)Loc.main.Escape)));
			flag2 = num3 < orbit.periapsisPassageTime;
			flag3 = false;
			DrawOrbit(orbit, trueAnomaly2, endTrueAnomaly2, startText2, endText2, c, flag4 ? 0.1f : 0.25f, 0.25f, lineDrawer);
			break;
		}
		case PathType.Encounter:
		{
			double num = Math.Max(WorldTime.main.worldTime, orbit.PathStartTime);
			double pathEndTime = orbit.PathEndTime;
			double trueAnomaly = orbit.GetTrueAnomaly(num);
			double endTrueAnomaly = orbit.GetTrueAnomaly(pathEndTime) + Math.PI * 2.0 * (double)orbit.direction * 10.0;
			bool flag = LanguageSettings.main.settings.name == "" || LanguageSettings.main.settings.name == "English";
			string startText = ((!drawStartText || !(num > WorldTime.main.worldTime)) ? null : ((!(Kepler.GetRadiusAtTrueAnomaly(orbit.slr, orbit.ecc, trueAnomaly) < orbit.Planet.SOI * 0.9999)) ? (flag ? "Encounter" : ((string)Loc.main.Encounter)) : (flag ? "Escape" : ((string)Loc.main.Escape))));
			string endText = (drawEndText ? orbit.encounterText() : null);
			flag2 = Math_Utility.IsInsideRange(orbit.GetNextTrueAnomalyPassTime(num, 0.0), num, pathEndTime);
			flag3 = Math_Utility.IsInsideRange(orbit.GetNextTrueAnomalyPassTime(num, Math.PI), num, pathEndTime);
			DrawOrbit(orbit, trueAnomaly, endTrueAnomaly, startText, endText, c, 0.1f, 0.25f, lineDrawer);
			break;
		}
		default:
			throw new Exception();
		}
		if (drawStats)
		{
			if (flag2)
			{
				DrawHeight(orbit, c, orbit.periapsis, 0.0);
			}
			if (flag3)
			{
				DrawHeight(orbit, c, orbit.apoapsis, Math.PI);
			}
		}
	}

	public static float GetOrbitAlpha(Orbit orbit)
	{
		double num = Math.Min(orbit.apoapsis, orbit.Planet.SOI);
		return Map.drawer.alphaPerDepth[orbit.Planet.orbitalDepth] * MapDrawer.GetFadeOut((double)Map.view.view.distance / 70.0, num * 0.75, num);
	}

	private static void DrawOrbit(Orbit orbit, double startTrueAnomaly, double endTrueAnomaly, string startText, string endText, Color c, float startAlpha, float endAlpha, LineDrawer lineDrawer)
	{
		Vector3[] points = orbit.GetPoints(startTrueAnomaly, endTrueAnomaly, 250, 0.001);
		lineDrawer.DrawLine(points, orbit.Planet, c * new Color(1f, 1f, 1f, startAlpha), c * new Color(1f, 1f, 1f, endAlpha));
		if (startText != null)
		{
			MapDrawer.DrawPointWithText(15, c, startText, 40, c, orbit.Planet.mapHolder.position + points.First(), -orbit.GetVelocityAtTrueAnomaly(endTrueAnomaly).ToVector2.normalized, 4, 4);
		}
		if (endText != null)
		{
			MapDrawer.DrawPointWithText(15, c, endText, 40, c, orbit.Planet.mapHolder.position + points.Last(), orbit.GetVelocityAtTrueAnomaly(endTrueAnomaly).ToVector2.normalized, 4, 4);
		}
	}

	private static void DrawHeight(Orbit orbit, Color color, double radius, double trueAnomaly)
	{
		if (!(radius < orbit.Planet.Radius))
		{
			Double2 @double = Double2.CosSin(trueAnomaly + orbit.arg);
			Vector2 position = MapDrawer.GetPosition(orbit.Planet, @double * radius);
			MapDrawer.DrawPointWithText(15, color, (radius - orbit.Planet.Radius).ToDistanceString(), 40, color, position, @double, 3, 3);
		}
	}
}
