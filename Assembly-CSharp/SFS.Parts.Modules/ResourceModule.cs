using System;
using System.Collections.Generic;
using SFS.Builds;
using SFS.Translations;
using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules;

[ExecuteInEditMode]
public class ResourceModule : MonoBehaviour, I_InitializePartModule, Rocket.INJ_Rocket, ResourceDrawer.I_Resource
{
	public ResourceType resourceType;

	public Double_Reference wetMass = new Double_Reference();

	public Double_Reference dryMassPercent = new Double_Reference();

	public Double_Reference resourcePercent = new Double_Reference();

	public bool setMass;

	public Double_Reference mass = new Double_Reference();

	[HideInInspector]
	public ResourceModule parent;

	[HideInInspector]
	public List<ResourceModule> children;

	[HideInInspector]
	public List<FlowModule.Flow> flowModules = new List<FlowModule.Flow>();

	public bool showDescription = true;

	public Rocket Rocket { private get; set; }

	private double DryMassMultiplier
	{
		get
		{
			if (!Application.isPlaying || !Base.worldBase.insideWorld.Value)
			{
				return 1.0;
			}
			return Base.worldBase.settings.difficulty.DryMassMultiplier;
		}
	}

	private double DryMassPercent => dryMassPercent.Value * DryMassMultiplier;

	public double TotalResourceCapacity => (1.0 - DryMassPercent) * wetMass.Value;

	public double ResourceAmount => TotalResourceCapacity * resourcePercent.Value;

	public double ResourceSpace => TotalResourceCapacity * (1.0 - resourcePercent.Value);

	int I_InitializePartModule.Priority => -1;

	ResourceType ResourceDrawer.I_Resource.ResourceType => resourceType;

	float ResourceDrawer.I_Resource.WetMass => (float)wetMass.Value;

	Double_Reference ResourceDrawer.I_Resource.ResourcePercent => resourcePercent;

	public void Draw(List<ResourceModule> modules, StatsMenu drawer, PartDrawSettings settings)
	{
		if (settings.build || settings.game)
		{
			drawer.DrawSlider(0, GetLabelAndValue, MaxSize, () => (float)resourcePercent.Value, SetResource, Register, Unregister);
		}
		else
		{
			drawer.DrawStat(0, GetLabelAndValue, MaxSize, Register, Unregister);
		}
		string GetLabelAndValue()
		{
			return Loc.main.Info_Resource_Amount.InjectField(resourceType.displayName, "resource").Inject(ResourceAmount.ToString(2, forceDecimals: false) + resourceType.resourceUnit.Field, "amount");
		}
		string MaxSize()
		{
			return Loc.main.Info_Resource_Amount.InjectField(resourceType.displayName, "resource").Inject(TotalResourceCapacity.ToString(2, forceDecimals: false) + resourceType.resourceUnit.Field, "amount");
		}
		void Register(Action update)
		{
			resourcePercent.OnChange += update;
		}
		void SetResource(float newValue, bool touchStart)
		{
			Undo.main.RecordStatChangeStep(modules, delegate
			{
				if (BuildManager.main != null)
				{
					foreach (ResourceModule module in modules)
					{
						module.resourcePercent.Value = newValue;
					}
				}
			}, touchStart);
		}
		void Unregister(Action update)
		{
			resourcePercent.OnChange -= update;
		}
	}

	void I_InitializePartModule.Initialize()
	{
		if (setMass)
		{
			wetMass.OnChange += new Action(RecalculateMass);
			dryMassPercent.OnChange += new Action(RecalculateMass);
			resourcePercent.OnChange += new Action(RecalculateMass);
		}
		resourcePercent.OnChange += new Action<double, double>(UpdateFlowModules);
	}

	private void RecalculateMass()
	{
		mass.Value = DryMassPercent * wetMass.Value + ResourceAmount * resourceType.resourceMass;
	}

	private void UpdateFlowModules(double oldAmount, double newAmount)
	{
		if (oldAmount != 1.0 && oldAmount != 0.0)
		{
			return;
		}
		foreach (FlowModule.Flow flowModule in flowModules)
		{
			flowModule.UpdateState();
		}
	}

	public void TakeResource(double takeAmount)
	{
		if (resourcePercent.Value != 0.0)
		{
			takeAmount = Math.Min(takeAmount, ResourceAmount);
			TakeFromParent(takeAmount);
			TakeFromChildren(1.0 - takeAmount / ResourceAmount);
			resourcePercent.Value -= takeAmount / TotalResourceCapacity;
		}
	}

	private void TakeFromChildren(double leftoverPercent)
	{
		foreach (ResourceModule child in children)
		{
			child.resourcePercent.Value *= leftoverPercent;
			child.TakeFromChildren(leftoverPercent);
		}
	}

	private void TakeFromParent(double amount)
	{
		if (!(parent == null))
		{
			parent.resourcePercent.Value -= amount / parent.TotalResourceCapacity;
			parent.TakeFromParent(amount);
		}
	}

	public void AddResource(double amount)
	{
		if (resourcePercent.Value != 1.0)
		{
			amount = Math.Min(amount, TotalResourceCapacity - ResourceAmount);
			AddToParent(amount);
			AddToChildren(amount / (TotalResourceCapacity - ResourceAmount));
			resourcePercent.Value += amount / TotalResourceCapacity;
		}
	}

	private void AddToChildren(double addPercent)
	{
		foreach (ResourceModule child in children)
		{
			child.resourcePercent.Value += (1.0 - child.resourcePercent.Value) * addPercent;
			child.AddToChildren(addPercent);
		}
	}

	private void AddToParent(double amount)
	{
		if (!(parent == null))
		{
			parent.resourcePercent.Value += amount / parent.TotalResourceCapacity;
			parent.AddToParent(amount);
		}
	}

	public void ToggleTransfer()
	{
		Part componentInParentTree = base.transform.GetComponentInParentTree<Part>();
		Rocket.resources.ToggleTransfer(componentInParentTree, parent);
	}

	public static ResourceModule CreateGroup(List<ResourceModule> resources, GameObject holder)
	{
		ResourceModule resourceModule = holder.AddComponent<ResourceModule>();
		resourceModule.resourceType = resources[0].resourceType;
		resourceModule.SetChildren(resources);
		((I_InitializePartModule)resourceModule).Initialize();
		return resourceModule;
	}

	private void SetChildren(List<ResourceModule> newChildren)
	{
		children = newChildren;
		double num = 0.0;
		double num2 = 0.0;
		foreach (ResourceModule newChild in newChildren)
		{
			num += newChild.TotalResourceCapacity;
			num2 += newChild.ResourceAmount;
			newChild.parent = this;
		}
		wetMass.Value = num;
		dryMassPercent.Value = 0.0;
		resourcePercent.Value = num2 / num;
	}
}
