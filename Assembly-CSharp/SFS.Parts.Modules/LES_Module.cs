using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class LES_Module : MonoBehaviour, Rocket.INJ_Rocket
{
	public Bool_Reference autoDetach;

	public Rocket Rocket { get; set; }

	public void Draw(List<LES_Module> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.build || settings.game)
		{
			drawer.DrawToggle(0, () => "Auto Detach Capsule", Toggle, () => autoDetach.Value, delegate(Action update)
			{
				autoDetach.OnChange += update;
			}, delegate(Action update)
			{
				autoDetach.OnChange -= update;
			});
		}
		void Toggle()
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				bool value = !autoDetach.Value;
				foreach (LES_Module module in modules)
				{
					module.autoDetach.Value = value;
				}
			});
		}
	}

	public void Activate(UsePartData usePartData)
	{
		if (!autoDetach.Value)
		{
			return;
		}
		EngineModule[] modules = Rocket.partHolder.GetModules<EngineModule>();
		for (int i = 0; i < modules.Length; i++)
		{
			modules[i].engineOn.Value = false;
		}
		int num = 0;
		while (Detach(usePartData))
		{
			if (num < 1000)
			{
				num++;
				continue;
			}
			throw new Exception("Infinite loop");
		}
	}

	private bool Detach(UsePartData usePartData)
	{
		JointGroup jointsGroup = Rocket.jointsGroup;
		Part componentInParent = GetComponentInParent<Part>();
		Queue<Part> queue = new Queue<Part>();
		HashSet<Part> hashSet = new HashSet<Part> { componentInParent };
		queue.Enqueue(componentInParent);
		while (queue.Count > 0)
		{
			Part a = queue.Dequeue();
			Part[] array = (from j in jointsGroup.GetConnectedJoints(a)
				select j.GetOtherPart(a)).ToArray();
			foreach (Part part in array)
			{
				if (hashSet.Contains(part))
				{
					continue;
				}
				if (part.HasModule<DetachModule>())
				{
					DetachModule[] modules = part.GetModules<DetachModule>();
					if (modules.Any((DetachModule m) => m.activatedByLES))
					{
						DetachModule[] array2 = modules;
						foreach (DetachModule detachModule in array2)
						{
							if (detachModule.activatedByLES)
							{
								detachModule.Detach(usePartData);
							}
						}
						PlayerController.main.SetOffset(Vector2.zero, 0.5f);
						return true;
					}
				}
				queue.Enqueue(part);
				hashSet.Add(part);
			}
		}
		return false;
	}
}
