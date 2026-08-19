using System;
using System.Collections.Generic;
using System.Linq;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class FuelPipeModule : MonoBehaviour
{
	public SurfaceData surface_In;

	public SurfaceData surface_Out;

	private List<FuelPipeModule> previousPipes = new List<FuelPipeModule>();

	private ResourceModule resource_In;

	private ResourceModule resource_Out;

	public void FindNeighbours(JointGroup group)
	{
		previousPipes.Clear();
		resource_In = null;
		resource_Out = null;
		Part componentInParent = GetComponentInParent<Part>();
		foreach (PartJoint connectedJoint in group.GetConnectedJoints(componentInParent))
		{
			Part otherPart = connectedJoint.GetOtherPart(componentInParent);
			if (otherPart.HasModule<FuelPipeModule>() && SurfaceUtility.SurfacesConnect(otherPart.GetModules<FuelPipeModule>()[0].surface_Out, surface_In, out var overlap, out var center))
			{
				previousPipes.Add(otherPart.GetModules<FuelPipeModule>()[0]);
			}
			if (otherPart.HasModule<ResourceModule>())
			{
				if (SurfaceUtility.SurfacesConnect(otherPart, surface_In, out overlap, out center))
				{
					resource_In = otherPart.GetModules<ResourceModule>()[0].parent;
				}
				if (SurfaceUtility.SurfacesConnect(otherPart, surface_Out, out overlap, out center))
				{
					resource_Out = otherPart.GetModules<ResourceModule>()[0].parent;
				}
			}
		}
	}

	public (ResourceModule[], ResourceModule)? FindFlow()
	{
		if (resource_Out == null)
		{
			return null;
		}
		ResourceModule[] array = (from a in FindFromTanks()
			where a.resourceType == resource_Out.resourceType
			select a).ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		return (array, resource_Out);
	}

	public List<ResourceModule> FindFlowsForEngine(FlowModule.FlowType flowType)
	{
		if (flowType == FlowModule.FlowType.Negative)
		{
			return FindFromTanks();
		}
		throw new NotImplementedException();
	}

	private List<ResourceModule> FindFromTanks()
	{
		List<FuelPipeModule> list = new List<FuelPipeModule>();
		Stack<FuelPipeModule> stack = new Stack<FuelPipeModule>();
		stack.Push(this);
		List<ResourceModule> list2 = new List<ResourceModule>();
		while (stack.Count > 0)
		{
			FuelPipeModule fuelPipeModule = stack.Pop();
			if (fuelPipeModule.resource_In != null && !list2.Contains(resource_In))
			{
				list2.Add(fuelPipeModule.resource_In);
			}
			foreach (FuelPipeModule previousPipe in fuelPipeModule.previousPipes)
			{
				if (!list.Contains(previousPipe))
				{
					list.Add(previousPipe);
					stack.Push(previousPipe);
				}
			}
		}
		return list2;
	}

	public static void FixedUpdate_FuelPipeFlow(List<(ResourceModule[] froms, ResourceModule to)> flows)
	{
		(ResourceModule[], ResourceModule)[] array = flows.Where(((ResourceModule[] froms, ResourceModule to) f) => f.froms.Any((ResourceModule a) => a.ResourceAmount > 0.0) && f.to.ResourceSpace > 0.0).ToArray();
		Dictionary<ResourceModule, int> dictionary = new Dictionary<ResourceModule, int>();
		Dictionary<ResourceModule, int> dictionary2 = new Dictionary<ResourceModule, int>();
		(ResourceModule[], ResourceModule)[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			(ResourceModule[], ResourceModule) tuple = array2[num];
			ResourceModule[] item = tuple.Item1;
			ResourceModule item2 = tuple.Item2;
			ResourceModule[] array3 = item;
			foreach (ResourceModule key in array3)
			{
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, 0);
				}
				dictionary[key]++;
			}
			if (!dictionary2.ContainsKey(item2))
			{
				dictionary2.Add(item2, 0);
			}
			dictionary2[item2]++;
		}
		Dictionary<ResourceModule, double> allowedToTake = dictionary.ToDictionary((KeyValuePair<ResourceModule, int> a) => a.Key, (KeyValuePair<ResourceModule, int> b) => b.Key.ResourceAmount / (double)b.Value);
		Dictionary<ResourceModule, double> dictionary3 = dictionary2.ToDictionary((KeyValuePair<ResourceModule, int> a) => a.Key, (KeyValuePair<ResourceModule, int> b) => b.Key.ResourceSpace / (double)b.Value);
		array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			(ResourceModule[], ResourceModule) tuple2 = array2[num];
			ResourceModule[] item3 = tuple2.Item1;
			ResourceModule item4 = tuple2.Item2;
			double num3 = Math.Min(item4.resourceType.transferRate * (double)Time.fixedDeltaTime, Math.Min(item3.Sum((ResourceModule from) => allowedToTake[from]), dictionary3[item4]));
			ResourceModule[] array3 = item3;
			for (int num2 = 0; num2 < array3.Length; num2++)
			{
				array3[num2].TakeResource(num3 / (double)item3.Length);
			}
			item4.AddResource(num3);
		}
	}
}
