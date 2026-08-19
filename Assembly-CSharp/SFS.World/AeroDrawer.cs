using System;
using System.Collections.Generic;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.World;

public class AeroDrawer : MonoBehaviour
{
	public TemperatureBar[] bars;

	public RectTransform holder;

	public RectTransform attachPoint;

	private Aero_Local aero = new Aero_Local();

	private int drawnCount = 3;

	private void Start()
	{
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
		WorldTime.main.realtimePhysics.OnChange += new Action(OnPlayerChange);
	}

	private void OnDestroy()
	{
		PlayerController.main.player.OnChange -= new Action(OnPlayerChange);
		WorldTime.main.realtimePhysics.OnChange -= new Action(OnPlayerChange);
	}

	private void OnPlayerChange()
	{
		bool flag = WorldTime.main.realtimePhysics.Value || true;
		Player value = PlayerController.main.player.Value;
		Aero_Local aero_Local = aero;
		Rocket rocket = value as Rocket;
		aero_Local.Value = ((((object)rocket != null) & flag) ? rocket.aero : ((value is Astronaut_EVA astronaut_EVA) ? astronaut_EVA.aero : null));
		base.enabled = aero.Value != null;
		if (base.enabled)
		{
			Draw();
		}
		else
		{
			holder.gameObject.SetActive(value: false);
		}
	}

	private void LateUpdate()
	{
		Draw();
	}

	private void Draw()
	{
		List<HeatModuleBase> list = ((aero.Value != null) ? aero.Value.heatManager.GetMostHeatedModules(bars.Length) : new List<HeatModuleBase>());
		list.Sort((HeatModuleBase a, HeatModuleBase b) => -a.IsHeatShield.CompareTo(b.IsHeatShield));
		for (int num = 0; num < Math.Max(drawnCount, list.Count); num++)
		{
			TemperatureBar temperatureBar = bars[num];
			bool flag = num < list.Count;
			if (temperatureBar.holders[0].activeSelf != flag)
			{
				GameObject[] holders = temperatureBar.holders;
				for (int num2 = 0; num2 < holders.Length; num2++)
				{
					holders[num2].SetActive(flag);
				}
			}
			if (flag)
			{
				HeatModuleBase heatModuleBase = list[num];
				temperatureBar.bar.fillAmount = heatModuleBase.Temperature / heatModuleBase.HeatTolerance;
				temperatureBar.temperatureText.Text = heatModuleBase.Name;
				temperatureBar.temperatureDegree.Text = heatModuleBase.Temperature.ToTemperatureString();
			}
		}
		drawnCount = list.Count;
		if (holder.gameObject.activeSelf != drawnCount > 0)
		{
			holder.gameObject.SetActive(drawnCount > 0);
		}
	}
}
