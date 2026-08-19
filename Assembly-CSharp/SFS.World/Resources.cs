using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.World;

public class Resources : ObservableMonoBehaviour
{
	[Serializable]
	public class Transfer
	{
		public Part part;

		public ResourceModule group;

		public Transfer(Part part, ResourceModule group)
		{
			this.part = part;
			this.group = group;
		}
	}

	public GameObject groupsHolder;

	public ResourceModule[] localGroups = new ResourceModule[0];

	public ResourceModule[] globalGroups = new ResourceModule[0];

	public BoosterModule[] boosters;

	public List<Transfer> transfers = new List<Transfer>();

	public Action onGroupsSetup;

	public void SetupResourceGroups(Rocket rocket)
	{
		ResourceModule[] array = globalGroups;
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i]);
		}
		array = localGroups;
		for (int i = 0; i < array.Length; i++)
		{
			UnityEngine.Object.Destroy(array[i]);
		}
		List<ResourceModule> list = new List<ResourceModule>();
		Dictionary<ResourceType, List<ResourceModule>> dictionary = new Dictionary<ResourceType, List<ResourceModule>>();
		foreach (List<ResourceModule> resourceGroup in rocket.jointsGroup.GetResourceGroups())
		{
			ResourceModule resourceModule = ResourceModule.CreateGroup(resourceGroup, groupsHolder);
			list.Add(resourceModule);
			if (!dictionary.ContainsKey(resourceModule.resourceType))
			{
				dictionary.Add(resourceModule.resourceType, new List<ResourceModule>());
			}
			dictionary[resourceModule.resourceType].Add(resourceModule);
		}
		localGroups = list.ToArray();
		boosters = rocket.partHolder.GetModules<BoosterModule>();
		globalGroups = dictionary.Values.Select((List<ResourceModule> collection) => ResourceModule.CreateGroup(collection, groupsHolder)).ToArray();
		onGroupsSetup?.Invoke();
	}

	private void FixedUpdate()
	{
		if (transfers.Count == 2 && !(transfers[0].group.resourceType != transfers[1].group.resourceType))
		{
			double val = (double)WorldTime.FixedDeltaTime * transfers[1].group.resourceType.transferRate;
			double resourceAmount = transfers[0].group.ResourceAmount;
			double val2 = transfers[1].group.TotalResourceCapacity - transfers[1].group.ResourceAmount;
			val = Math.Min(val, Math.Min(resourceAmount, val2)) * 1.000001;
			if (val > 0.0)
			{
				transfers[0].group.TakeResource(val);
				transfers[1].group.AddResource(val);
			}
		}
	}

	public void ToggleTransfer(Part part, ResourceModule group)
	{
		Transfer[] array = transfers.ToArray();
		foreach (Transfer transfer in array)
		{
			if (transfer.group == group)
			{
				transfers.Remove(transfer);
				return;
			}
		}
		transfers.Add(new Transfer(part, group));
		if (transfers.Count > 2)
		{
			transfers.RemoveAt(0);
		}
	}

	public void RemoveInvalidTransfers(PartHolder holder)
	{
		for (int num = transfers.Count - 1; num >= 0; num--)
		{
			if (!holder.ContainsPart(transfers[num].part))
			{
				transfers.Remove(transfers[num]);
			}
			else
			{
				transfers[num].group = transfers[num].part.GetModules<ResourceModule>()[0].parent;
			}
		}
	}
}
