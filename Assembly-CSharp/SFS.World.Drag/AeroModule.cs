using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.World.Drag;

public abstract class AeroModule : MonoBehaviour
{
	public HeatManager heatManager;

	public BurnManager burnManager;

	[Space]
	public AeroMesh shockEdge;

	public AeroMesh shockOuter;

	[Space]
	public AeroMesh reentryEdge;

	public AeroMesh reentryOuter;

	[Space]
	public AudioModule airflowSound;

	public AudioModule burnSound;

	private int frameIndex;

	protected abstract bool PhysicsMode { get; }

	private void FixedUpdate()
	{
		frameIndex++;
		bool drewReentryMesh = false;
		Location location = GetLocation();
		if (IsInsideAtmosphereAndIsMoving(location))
		{
			GetTemperatureAndShockwave(location, out var Q, out var _, out var temperature);
			bool flag = PhysicsMode && !SandboxSettings.main.settings.noAtmosphericDrag;
			bool flag2 = temperature > 0f;
			float g_ForSound = 0f;
			if (flag | flag2)
			{
				float num = (float)location.velocity.AngleRadians - (float)Math.PI / 2f;
				Matrix2x2 rotate = Matrix2x2.Angle(0f - num);
				Matrix2x2 localToWorld = Matrix2x2.Angle(num);
				List<Surface> exposedSurfaces = GetExposedSurfaces(GetDragSurfaces(rotate));
				if (flag)
				{
					ApplyForce(exposedSurfaces, location, localToWorld, out g_ForSound);
				}
				if (flag2)
				{
					FixedUpdate_Reentry_And_Heating(temperature, exposedSurfaces, num, localToWorld, ref drewReentryMesh);
				}
			}
			airflowSound.volume.Value = Q / (Q + 100f) + g_ForSound / (g_ForSound + 1f) * 0.3f;
			airflowSound.pitch.Value = 0.4f + (float)(location.velocity.magnitude / (location.velocity.magnitude + 1000.0)) * 0.6f;
			burnSound.volume.Value = Formula(temperature, 10000f);
		}
		else
		{
			airflowSound.volume.Value = 0f;
			burnSound.volume.Value = 0f;
		}
		heatManager.DissipateHeat(frameIndex);
		if (reentryEdge.gameObject.activeSelf != drewReentryMesh)
		{
			reentryEdge.gameObject.SetActive(drewReentryMesh);
			reentryOuter.gameObject.SetActive(drewReentryMesh);
		}
		static float Formula(float a, float center)
		{
			if (!(a < center))
			{
				return a / (a + center);
			}
			return Mathf.InverseLerp(0f, center, a) * 0.5f;
		}
	}

	public static bool IsInsideAtmosphereAndIsMoving(Location location)
	{
		if (location.planet.IsInsideAtmosphere(location.position))
		{
			return location.velocity.Mag_MoreThan(1.0);
		}
		return false;
	}

	private void ApplyForce(List<Surface> exposedSurfaces, Location location, Matrix2x2 localToWorld, out float g_ForSound)
	{
		(float drag, Vector2 centerOfDrag) tuple = CalculateDragForce(exposedSurfaces);
		float item = tuple.drag;
		Vector2 item2 = tuple.centerOfDrag;
		float num = (float)location.planet.GetAtmosphericDensity(location.Height);
		float force = item * 1.5f * (float)location.velocity.sqrMagnitude;
		Vector2 centerOfDrag_World = localToWorld * item2;
		g_ForSound = force * num / GetMass() / 9.8f;
		if (this is Aero_Rocket aero_Rocket)
		{
			centerOfDrag_World = Vector2.Lerp(aero_Rocket.rocket.rb2d.worldCenterOfMass, centerOfDrag_World, 0.2f);
			aero_Rocket.ApplyParachuteDrag(ref force, ref centerOfDrag_World);
		}
		Vector2 force2 = -location.velocity.ToVector2.normalized * (force * num);
		if (!float.IsNaN(force2.x + force2.y + centerOfDrag_World.x + centerOfDrag_World.y))
		{
			AddForceAtPosition(force2, centerOfDrag_World);
		}
	}

	private static (float drag, Vector2 centerOfDrag) CalculateDragForce(List<Surface> surfaces)
	{
		float num = 0f;
		Vector2 zero = Vector2.zero;
		foreach (Surface surface in surfaces)
		{
			Vector2 vector = surface.line.end - surface.line.start;
			if (!(vector.x < 0.01f))
			{
				float num2 = vector.x / (vector.x + Mathf.Abs(vector.y));
				float num3 = vector.x * num2;
				num += num3;
				zero += (surface.line.start + surface.line.end) * num3;
			}
		}
		if (num > 0f)
		{
			zero /= num * 2f;
		}
		return (drag: num, centerOfDrag: zero);
	}

	private void FixedUpdate_Reentry_And_Heating(float temperature, List<Surface> exposedSurfaces, float velocityAngleRad, Matrix2x2 localToWorld, ref bool drewReentryMesh)
	{
		if (!(temperature <= 0f))
		{
			if (!SandboxSettings.main.settings.noBurnMarks && this is Aero_Rocket)
			{
				burnManager.ApplyBurnMarks(exposedSurfaces, temperature, localToWorld, velocityAngleRad, frameIndex);
			}
			exposedSurfaces = RemoveHighSlopeSurfaces(exposedSurfaces, 5f);
			ApplyProtectionZone(exposedSurfaces);
			heatManager.ApplyHeat(exposedSurfaces, temperature, frameIndex);
			AeroData aeroData = GameManager.main.aeroData;
			drewReentryMesh = true;
			AeroMesh.Data a = new AeroMesh.Data
			{
				velocityAngle_Rad = velocityAngleRad,
				localToWorld = localToWorld,
				data = aeroData.reentry_Edge,
				curveData = aeroData.reentry_Edge.GetCurveData()
			};
			reentryEdge.GenerateMesh(a, exposedSurfaces);
			reentryEdge.SetTemperature(GetIntensity(temperature, 1000f) * 0.7f);
			AeroMesh.Data a2 = new AeroMesh.Data
			{
				velocityAngle_Rad = velocityAngleRad,
				localToWorld = localToWorld,
				data = aeroData.reentry_Outer,
				curveData = aeroData.reentry_Outer.GetCurveData(exposedSurfaces.Count),
				sampleCurve = aeroData.reentry_Outer.SampleCurve()
			};
			reentryOuter.GenerateMesh(a2, exposedSurfaces);
			reentryOuter.SetTemperature(GetIntensity(temperature, 1800f) * 0.6f);
		}
	}

	public static void GetTemperatureAndShockwave(Location location, out float Q, out float shockOpacity, out float temperature)
	{
		AeroData aeroData = GameManager.main.aeroData;
		if (aeroData.testShock || aeroData.testReentry)
		{
			Q = 0f;
			shockOpacity = (aeroData.testShock ? (aeroData.shockOpacity / 100f) : 0f);
			temperature = (aeroData.testReentry ? (aeroData.reentryPercent / (100f - aeroData.reentryPercent) * 3000f) : 0f);
		}
		else
		{
			double magnitude = location.velocity.magnitude;
			double verticalVelocity = location.VerticalVelocity;
			double atmosphericDensity = location.planet.GetAtmosphericDensity(location.Height);
			aeroData.Formula.GetEverything(magnitude, verticalVelocity, atmosphericDensity, location.planet.data.atmospherePhysics.minHeatingVelocityMultiplier, location.planet.data.atmospherePhysics.shockwaveIntensity, out Q, out shockOpacity, out temperature, out var _);
		}
	}

	public static float GetIntensity(float value, float halfPoint)
	{
		return value / (value + halfPoint);
	}

	public static float GetHeatTolerance(HeatTolerance a)
	{
		return a switch
		{
			HeatTolerance.High => 6000, 
			HeatTolerance.Mid => 1000, 
			HeatTolerance.Low => 400, 
			_ => 0, 
		};
	}

	public static List<Surface> GetExposedSurfaces(List<Surface> surfaces)
	{
		if (surfaces.Count == 0)
		{
			return new List<Surface>();
		}
		SortDragSurfacesByEndX(surfaces);
		List<Surface> output = new List<Surface> { surfaces.First() };
		for (int i = 1; i < surfaces.Count; i++)
		{
			Surface surface = surfaces[i];
			int num = output.Count - 1;
			bool flag = surface.line.end.x > output.Last().line.end.x + 0.001f;
			if (flag)
			{
				output.Add(new Surface(surface.owner, surface.valid, new Line2(surface.line.GetPositionAtX_Unclamped(Mathf.Max(output.Last().line.end.x, surface.line.start.x)), surface.line.end)));
			}
			int index;
			for (index = num; index >= 0; index--)
			{
				Surface SECTION;
				Surface surface2 = (SECTION = output[index]);
				if (surface.line.start.x > SECTION.line.end.x - 0.001f)
				{
					break;
				}
				Vector2 positionAtX_Unclamped = surface.line.GetPositionAtX_Unclamped(surface2.line.end.x);
				float f = positionAtX_Unclamped.y - surface2.line.end.y;
				int num2 = ((Mathf.Abs(f) > 0.001f) ? ((int)Mathf.Sign(f)) : 0);
				if (num2 == 1)
				{
					if (flag)
					{
						SetSectionEnd(output[index + 1].owner, output[index + 1].line.end);
						RemoveSection(index + 1);
					}
					else
					{
						SetSectionEnd(surface.owner, positionAtX_Unclamped);
					}
				}
				bool num3 = surface.line.start.x < surface2.line.start.x + 0.001f;
				Vector2 vector = (num3 ? surface.line.GetPositionAtX_Unclamped(surface2.line.start.x) : surface.line.start);
				Vector2 vector2 = (num3 ? surface2.line.start : surface2.line.GetPositionAtX_Unclamped(surface.line.start.x));
				float f2 = vector.y - vector2.y;
				int num4 = ((Mathf.Abs(f2) > 0.001f) ? ((int)Mathf.Sign(f2)) : 0);
				if (num2 != num4 && Line2.FindIntersection_Unclamped(surface.line, surface2.line, out var position))
				{
					InsertSection(index + 1, SECTION.owner, position, SECTION.line.end);
					SetSectionEnd(SECTION.owner, position);
				}
				if (num3)
				{
					if (num4 >= 0)
					{
						Vector2 start = ((index > 0 && surface.line.start.x < output[index - 1].line.end.x + 0.001f) ? surface.line.GetPositionAtX_Unclamped(output[index - 1].line.end.x) : ((!(surface.line.start.x < SECTION.line.start.x - 0.001f)) ? surface.line.GetPositionAtX_Unclamped(SECTION.line.start.x) : surface.line.start));
						SetSectionStart(surface.owner, start);
					}
					else
					{
						bool num5 = (index == 0 || output[index - 1].line.end.x + 0.001f < SECTION.line.start.x) && surface.line.start.x < SECTION.line.start.x - 0.001f;
						float b = ((index > 0) ? output[index - 1].line.end.x : float.NegativeInfinity);
						float x = (num5 ? Mathf.Max(surface.line.start.x, b) : float.NaN);
						if (num5)
						{
							InsertSection(index, surface.owner, surface.line.GetPositionAtX_Unclamped(x), surface.line.GetPositionAtX_Unclamped(SECTION.line.start.x));
							num4 = 1;
						}
					}
				}
				else if (num4 >= 0)
				{
					InsertSection(index + 1, SECTION.owner, surface.line.start, SECTION.line.end);
					SetSectionEnd(surface2.owner, surface2.line.GetPositionAtX_Unclamped(surface.line.start.x));
				}
				flag = num4 == 1;
				void SetSectionEnd(HeatModuleBase part, Vector2 end)
				{
					SECTION.owner = part;
					SECTION.line.end = end;
					output[index] = SECTION;
				}
				void SetSectionStart(HeatModuleBase part, Vector2 start2)
				{
					SECTION.owner = part;
					SECTION.line.start = start2;
					output[index] = SECTION;
				}
			}
		}
		return output;
		void InsertSection(int ii, HeatModuleBase part, Vector2 start2, Vector2 end)
		{
			output.Insert(ii, new Surface(part, part.valid, new Line2(start2, end)));
		}
		void RemoveSection(int ii)
		{
			output.RemoveAt(ii);
		}
	}

	private static void SortDragSurfacesByEndX(List<Surface> surfaces)
	{
		int count = surfaces.Count;
		for (int num = count / 2; num > 0; num /= 2)
		{
			for (int i = 0; i + num < count; i++)
			{
				int num2 = i + num;
				Surface value = surfaces[num2];
				while (num2 - num >= 0 && value.line.end.x < surfaces[num2 - num].line.end.x)
				{
					surfaces[num2] = surfaces[num2 - num];
					num2 -= num;
				}
				surfaces[num2] = value;
			}
		}
	}

	private static List<Surface> RemoveHighSlopeSurfaces(List<Surface> surfaces, float maxSlope)
	{
		return surfaces.Where((Surface surface) => Mathf.Abs(surface.line.SizeY / surface.line.SizeX) < maxSlope && surface.line.SizeX > 0.1f).ToList();
	}

	private static void ApplyProtectionZone(List<Surface> surfaces)
	{
		for (int i = 1; i < surfaces.Count - 1; i++)
		{
			Line2 line = surfaces[i].line;
			float num = line.start.y - surfaces[i - 1].line.end.y;
			if (num > 0.1f)
			{
				float num2 = line.start.x - Mathf.Min(num * 0.2f, 0.4f);
				int num3 = i - 1;
				while (num3 >= 0)
				{
					if (surfaces[num3].line.start.x > num2)
					{
						surfaces.RemoveAt(num3);
						i--;
						num3--;
						continue;
					}
					Surface value = surfaces[num3];
					value.line.end = value.line.GetPositionAtX(num2);
					surfaces[num3] = value;
					break;
				}
			}
			if (!(line.end.y - surfaces[i + 1].line.start.y > 0.1f))
			{
				continue;
			}
			float num4 = line.end.x + Mathf.Min(num * 0.2f, 0.4f);
			int num5 = i + 1;
			while (num5 < surfaces.Count)
			{
				if (surfaces[num5].line.end.x < num4)
				{
					surfaces.RemoveAt(num5);
					num5--;
					num5++;
					continue;
				}
				Surface value2 = surfaces[num5];
				value2.line.start = value2.line.GetPositionAtX(num4);
				surfaces[num5] = value2;
				break;
			}
		}
		for (int num6 = surfaces.Count - 1; num6 >= 0; num6--)
		{
			if (surfaces[num6].line.SizeX < 0.1f)
			{
				surfaces.RemoveAt(num6);
			}
		}
	}

	public static Line2[] RotateSurfaces(List<Surface> surfaces, Matrix2x2 localToWorld)
	{
		Line2[] array = new Line2[surfaces.Count];
		for (int i = 0; i < surfaces.Count; i++)
		{
			Surface surface = surfaces[i];
			array[i] = new Line2(surface.line.start * localToWorld, surface.line.end * localToWorld);
		}
		return array;
	}

	protected abstract Location GetLocation();

	protected abstract List<Surface> GetDragSurfaces(Matrix2x2 rotate);

	protected abstract void AddForceAtPosition(Vector2 force, Vector2 position);

	protected abstract float GetMass();
}
