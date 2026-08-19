using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Translations;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

public class FlowModule : MonoBehaviour
{
	[Serializable]
	public class Flow
	{
		public ResourceType resourceType;

		public float flowPercent = 1f;

		public SourceMode sourceSearchMode;

		public SurfaceData surface;

		public FlowType flowType;

		[HideInInspector]
		public Double_Reference flowRate;

		public State_Local state = new State_Local();

		public ResourceModule[] sources = new ResourceModule[0];

		public void FindSources(Rocket rocket, Part part)
		{
			sources = GetSources(rocket, part);
			ResourceModule[] array = sources;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].flowModules.Add(this);
			}
			UpdateState();
		}

		private ResourceModule[] GetSources(Rocket rocket, Part part)
		{
			if (sourceSearchMode == SourceMode.Global)
			{
				return GetGlobally(rocket);
			}
			if (sourceSearchMode == SourceMode.Surfaces)
			{
				return GetBySurfaces(rocket.jointsGroup, part, forBuild: false);
			}
			if (sourceSearchMode == SourceMode.Local)
			{
				return GetLocally(part);
			}
			throw new Exception();
		}

		private ResourceModule[] GetGlobally(Rocket rocket)
		{
			return rocket.resources.globalGroups.Where((ResourceModule resourceModule) => resourceModule.resourceType == resourceType).ToArray();
		}

		public ResourceModule[] GetBySurfaces(JointGroup jointsGroup, Part part, bool forBuild)
		{
			List<ResourceModule> list = new List<ResourceModule>();
			foreach (PartJoint connectedJoint in jointsGroup.GetConnectedJoints(part))
			{
				Part otherPart = connectedJoint.GetOtherPart(part);
				if (otherPart.HasModule<ResourceModule>() && SurfaceUtility.SurfacesConnect(otherPart, surface, out var overlap, out var center))
				{
					ResourceModule resourceModule = otherPart.GetModules<ResourceModule>()[0];
					ResourceModule resourceModule2 = (forBuild ? resourceModule : resourceModule.parent);
					if (resourceModule2.resourceType == resourceType && !list.Contains(resourceModule2))
					{
						list.Add(resourceModule2);
					}
				}
				if (!otherPart.HasModule<FuelPipeModule>())
				{
					continue;
				}
				FuelPipeModule fuelPipeModule = otherPart.GetModules<FuelPipeModule>()[0];
				if (!SurfaceUtility.SurfacesConnect((flowType == FlowType.Negative) ? fuelPipeModule.surface_Out : fuelPipeModule.surface_In, surface, out overlap, out center))
				{
					continue;
				}
				if (forBuild)
				{
					list.Add(null);
					continue;
				}
				foreach (ResourceModule item in fuelPipeModule.FindFlowsForEngine(flowType))
				{
					if (item.resourceType == resourceType && !list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
			return list.ToArray();
		}

		private ResourceModule[] GetLocally(Part part)
		{
			return (from a in part.GetModules<ResourceModule>()
				where a.resourceType == resourceType
				select a).ToArray();
		}

		public void Initialize()
		{
			flowRate.OnChange += new Action(UpdateState);
		}

		public void UpdateState()
		{
			if (sources.Length == 0)
			{
				state.Value = FlowState.NoSource;
			}
			else if (flowType == FlowType.Negative && GetSourcesResourceAmount() == 0.0)
			{
				state.Value = ((!SandboxSettings.main.settings.infiniteFuel) ? FlowState.NoResource : FlowState.CanFlow);
			}
			else if (flowType == FlowType.Positive && GetSourcesResourceSpace() == 0.0)
			{
				state.Value = (SandboxSettings.main.settings.infiniteFuel ? FlowState.CanFlow : FlowState.NoSpace);
			}
			else
			{
				state.Value = ((flowRate.Value > 0.0) ? FlowState.IsFlowing : FlowState.CanFlow);
			}
		}

		public void OnFixedUpdate()
		{
			if (flowType == FlowType.Negative)
			{
				FlowNegative();
			}
			else if (flowType == FlowType.Positive)
			{
				FlowPositive();
			}
		}

		private void FlowNegative()
		{
			if (!SandboxSettings.main.settings.infiniteFuel)
			{
				double sourcesResourceAmount = GetSourcesResourceAmount();
				double num = Math.Min(flowRate.Value * (double)Time.fixedDeltaTime / sourcesResourceAmount, 1.0);
				ResourceModule[] array = sources;
				foreach (ResourceModule obj in array)
				{
					obj.TakeResource(obj.ResourceAmount * num);
				}
				if (num == 1.0)
				{
					UpdateState();
				}
			}
		}

		private void FlowPositive()
		{
			double sourcesResourceSpace = GetSourcesResourceSpace();
			double num = Math.Min(flowRate.Value * (double)Time.fixedDeltaTime / sourcesResourceSpace, 1.0);
			ResourceModule[] array = sources;
			foreach (ResourceModule obj in array)
			{
				obj.AddResource(obj.ResourceSpace * num);
			}
			if (num == 1.0)
			{
				UpdateState();
			}
		}

		private double GetSourcesResourceAmount()
		{
			return sources.Sum((ResourceModule source) => source.ResourceAmount);
		}

		private double GetSourcesResourceSpace()
		{
			return sources.Sum((ResourceModule source) => source.ResourceSpace);
		}

		public bool CanFlow_ElseShowMsg(I_MsgLogger logger)
		{
			if ((FlowState)state == FlowState.NoSource)
			{
				logger.Log(Loc.main.Msg_No_Resource_Source.InjectField(resourceType.displayName, "resource"));
			}
			if ((FlowState)state == FlowState.NoResource)
			{
				logger.Log(Loc.main.Msg_No_Resource_Left.InjectField(resourceType.displayName, "resource"));
			}
			if (state.Value != FlowState.CanFlow)
			{
				return state.Value == FlowState.IsFlowing;
			}
			return true;
		}
	}

	[Serializable]
	public class State_Local : Obs<FlowState>
	{
		protected override bool IsEqual(FlowState a, FlowState b)
		{
			return a == b;
		}
	}

	public enum SourceMode
	{
		Global,
		Surfaces,
		Local
	}

	public enum FlowType
	{
		Negative,
		Positive
	}

	public enum FlowState
	{
		NoSource,
		NoResource,
		NoSpace,
		CanFlow,
		IsFlowing
	}

	public Flow[] sources;

	private double massFlow = double.NaN;

	public event Action onStateChange;

	private void Start()
	{
		Flow[] array = sources;
		foreach (Flow obj in array)
		{
			obj.Initialize();
			obj.state.OnChange += (Action)delegate
			{
				onStateChange?.Invoke();
			};
		}
		onStateChange += UpdateEnabled;
	}

	public void SetMassFlow(double newMassFlow)
	{
		if (newMassFlow != massFlow)
		{
			massFlow = newMassFlow;
			double massFlowPerUnit = GetMassFlowPerUnit();
			double num = ((massFlowPerUnit > 0.0) ? (newMassFlow / massFlowPerUnit) : 1.0);
			Flow[] array = sources;
			foreach (Flow flow in array)
			{
				flow.flowRate.Value = num * (double)flow.flowPercent;
			}
			UpdateEnabled();
		}
	}

	private double GetMassFlowPerUnit()
	{
		return sources.Sum((Flow source) => source.resourceType.resourceMass * (double)source.flowPercent);
	}

	public void FindSources(Rocket rocket)
	{
		Part componentInParentTree = base.transform.GetComponentInParentTree<Part>();
		Flow[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].FindSources(rocket, componentInParentTree);
		}
	}

	public bool CanFlow(I_MsgLogger logger)
	{
		Flow[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].CanFlow_ElseShowMsg(logger))
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateEnabled()
	{
		sources.ForEach(delegate(Flow s)
		{
			s.UpdateState();
		});
		base.enabled = sources.Any((Flow source) => source.state.Value == FlowState.IsFlowing);
	}

	private void FixedUpdate()
	{
		Flow[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnFixedUpdate();
		}
	}
}
