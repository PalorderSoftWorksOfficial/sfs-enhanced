using System.Collections.Generic;
using System.Linq;
using SFS.Analytics;
using SFS.Parts;
using UnityEngine;

namespace SFS.World.Drag;

public class HeatManager : MonoBehaviour
{
	private const float AbsorptionRate = 0.02f;

	private const float DissipationRate = 0.01f;

	private List<HeatModuleBase> heated = new List<HeatModuleBase>();

	public void ApplyHeat(List<Surface> exposedSurfaces, float temperature, int frameIndex)
	{
		float num = 0.02f * WorldTime.FixedDeltaTime;
		for (int num2 = exposedSurfaces.Count - 1; num2 >= 0; num2--)
		{
			if (!exposedSurfaces[num2].valid.valid)
			{
				exposedSurfaces.RemoveAt(num2);
				AnalyticsUtility.SendEvent("Heat_Manager_Save_v2");
			}
		}
		foreach (Surface exposedSurface in exposedSurfaces)
		{
			exposedSurface.owner.ExposedSurface += exposedSurface.line.end.x - exposedSurface.line.start.x;
		}
		List<HeatModuleBase> list = new List<HeatModuleBase>();
		foreach (Surface exposedSurface2 in exposedSurfaces)
		{
			HeatModuleBase owner = exposedSurface2.owner;
			if (owner.LastAppliedIndex == frameIndex)
			{
				continue;
			}
			if (float.IsNegativeInfinity(owner.Temperature))
			{
				owner.Temperature = 0f;
				heated.Add(owner);
			}
			float num3 = temperature - owner.Temperature;
			if (!(num3 <= 0f))
			{
				float num4 = 1f + Mathf.Log10(owner.ExposedSurface + 1f);
				float num5 = ((num3 < 1000f) ? num3 : (num3 * num3 / 1000f));
				owner.Temperature += num4 * num5 * num;
				owner.LastAppliedIndex = frameIndex;
				if (owner.Temperature > owner.HeatTolerance * 1.03f && !SandboxSettings.main.settings.noHeatDamage && WorldTime.main.realtimePhysics.Value && !list.Contains(owner))
				{
					list.Add(owner);
				}
			}
		}
		foreach (Surface exposedSurface3 in exposedSurfaces)
		{
			exposedSurface3.owner.ExposedSurface = 0f;
		}
		foreach (HeatModuleBase item in list)
		{
			item.OnOverheat(breakup: true);
		}
	}

	public void DissipateHeat(int frameIndex)
	{
		float num = 0.01f * WorldTime.FixedDeltaTime;
		float num2 = 10f * WorldTime.FixedDeltaTime;
		for (int num3 = heated.Count - 1; num3 >= 0; num3--)
		{
			HeatModuleBase heatModuleBase = heated[num3];
			if (heatModuleBase.LastAppliedIndex != frameIndex)
			{
				float temperature = heatModuleBase.Temperature;
				if (temperature >= 0f)
				{
					heatModuleBase.Temperature -= num2 + temperature * num;
				}
				if (heatModuleBase.Temperature <= 0f)
				{
					heatModuleBase.Temperature = float.NegativeInfinity;
					heated.RemoveAt(num3);
				}
			}
		}
	}

	public void OnSetParts(Part[] newParts)
	{
		heated.Clear();
		for (int i = 0; i < newParts.Length; i++)
		{
			HeatModuleBase[] modules = newParts[i].GetModules<HeatModuleBase>();
			foreach (HeatModuleBase heatModuleBase in modules)
			{
				if (!float.IsNegativeInfinity(heatModuleBase.Temperature))
				{
					if (heatModuleBase.Temperature < 0f || heatModuleBase.Temperature > 1000000f)
					{
						heatModuleBase.Temperature = float.NegativeInfinity;
					}
					else
					{
						heated.Add(heatModuleBase);
					}
				}
			}
		}
	}

	public List<HeatModuleBase> GetMostHeatedModules(int count)
	{
		List<HeatModuleBase> best = new List<HeatModuleBase>();
		foreach (HeatModuleBase item in heated)
		{
			int num = GetTargetIndex(item);
			if (num <= count - 1)
			{
				best.Insert(num, item);
			}
		}
		return best.Take(Mathf.Min(best.Count, count)).ToList();
		static float GetHeatScore(HeatModuleBase module)
		{
			float num2 = module.Temperature / module.HeatTolerance;
			if (!module.IsHeatShield)
			{
				return num2;
			}
			return Mathf.Lerp(0.35f, 1f, num2);
		}
		int GetTargetIndex(HeatModuleBase module)
		{
			float num2 = GetHeatScore(module);
			int i;
			for (i = 0; i < best.Count; i++)
			{
				if (num2 - 0.01f > GetHeatScore(best[i]))
				{
					return i;
				}
			}
			return i;
		}
	}

	public void HeatPart(HeatModuleBase a)
	{
		if (float.IsNegativeInfinity(a.Temperature) || float.IsNaN(a.Temperature))
		{
			a.Temperature = 0f;
			heated.Add(a);
		}
		a.Temperature += 150f * Time.fixedDeltaTime;
		if (a.Temperature > a.HeatTolerance * 1.03f && !SandboxSettings.main.settings.noHeatDamage && WorldTime.main.realtimePhysics.Value)
		{
			a.OnOverheat(breakup: false);
		}
	}
}
