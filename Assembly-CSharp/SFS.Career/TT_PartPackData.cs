using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Career;

public class TT_PartPackData : MonoBehaviour, I_TechTreeData
{
	public string displayName;

	[Space]
	public VariantRef[] parts;

	public bool center;

	public double cost;

	bool I_TechTreeData.IsComplete
	{
		get
		{
			if (!CareerState.main.HasUpgrade(((I_TechTreeData)this).Name_ID))
			{
				return base.name == "Basics";
			}
			return true;
		}
	}

	bool I_TechTreeData.GrayOut => !((I_TechTreeData)this).IsComplete;

	string I_TechTreeData.Name_ID => base.name;

	public int Value => parts.Length;
}
