using System;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class ActiveModule : MonoBehaviour, I_InitializePartModule
{
	public Bool_Reference active;

	public bool invert;

	public int Priority => 5;

	public void Initialize()
	{
		active.OnChange += new Action(UpdateActive);
	}

	private void UpdateActive()
	{
		bool flag = active.Value;
		if (invert)
		{
			flag = !flag;
		}
		if (base.gameObject.activeSelf != flag)
		{
			base.gameObject.SetActive(flag);
		}
	}
}
