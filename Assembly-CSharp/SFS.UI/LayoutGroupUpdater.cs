using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class LayoutGroupUpdater : NewElement
{
	public UnityEngine.UI.LayoutGroup layoutGroup;

	public List<NewElement> children = new List<NewElement>();

	private void Awake()
	{
		foreach (NewElement child in children)
		{
			child.SetParent(this);
		}
	}

	public override void ActAsRoot()
	{
		foreach (NewElement child in children)
		{
			child.ActAsRoot();
		}
		if (layoutGroup.enabled)
		{
			layoutGroup.enabled = false;
			layoutGroup.enabled = true;
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetRect());
		}
	}

	protected override Vector2 GetPreferredSize_Internal()
	{
		throw new NotImplementedException();
	}

	protected override void SetSize_Internal(Vector2 size)
	{
		throw new NotImplementedException();
	}
}
