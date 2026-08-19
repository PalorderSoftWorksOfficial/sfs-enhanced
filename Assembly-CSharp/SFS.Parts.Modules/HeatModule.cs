using SFS.Translations;
using SFS.Variables;
using SFS.World.Drag;
using UnityEngine;

namespace SFS.Parts.Modules;

public class HeatModule : HeatModuleBase
{
	public HeatTolerance heatTolerance;

	public bool isHeatShield;

	[Space]
	public bool useCustomName;

	public TranslationVariable customName;

	[Space]
	public Float_Reference temperature = new Float_Reference
	{
		localValue = double.NegativeInfinity
	};

	private Part part;

	public override string Name
	{
		get
		{
			if (useCustomName)
			{
				return customName.Field;
			}
			if (part == null)
			{
				part = base.transform.GetComponentInParentTree<Part>();
			}
			return part.GetDisplayName();
		}
	}

	public override bool IsHeatShield => isHeatShield;

	public override float Temperature
	{
		get
		{
			return temperature.Value;
		}
		set
		{
			temperature.Value = value;
		}
	}

	public override int LastAppliedIndex { get; set; } = -1;

	public override float ExposedSurface { get; set; }

	public override float HeatTolerance => AeroModule.GetHeatTolerance(heatTolerance);

	private void Start()
	{
		part = base.transform.GetComponentInParentTree<Part>();
	}

	public override void OnOverheat(bool breakup)
	{
		part.OnOverheat(this, breakup);
	}
}
