using UnityEngine;

namespace SFS.Career;

public class TT_InfoData : MonoBehaviour, I_TechTreeData
{
	[Multiline]
	public string title;

	[Multiline]
	public string info;

	public string CodeName => base.name;

	public bool IsComplete => false;

	bool I_TechTreeData.GrayOut => !IsComplete;

	string I_TechTreeData.Name_ID => CodeName;

	public int Value => 0;
}
