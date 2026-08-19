using UnityEngine;

namespace SFS.Career;

public class TT_UpgradeData : MonoBehaviour, I_TechTreeData
{
	public string title;

	public string description;

	public double cost;

	public string CodeName => base.name;

	public bool IsComplete => CareerState.main.state.unlocked_Upgrades.Contains(CodeName);

	bool I_TechTreeData.GrayOut => !((I_TechTreeData)this).IsComplete;

	string I_TechTreeData.Name_ID => CodeName;

	public int Value => 1;
}
