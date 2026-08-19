using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Career;
using SFS.UI;
using UnityEngine;
using UnityEngine.UI;

public abstract class TT_Base : MonoBehaviour
{
	public SFS.UI.Button element;

	[Space]
	public GameObject unselectedHolder;

	public GameObject selectedHolder;

	public GameObject locked;

	public List<Image> grayOut;

	protected TreeComponent owner;

	private (Color, Color)[] elementColors;

	public Action onComplete;

	public abstract I_TechTreeData Data { get; }

	public void SetupParents(TreeComponent owner, TT_Base[] parents)
	{
		this.owner = owner;
		UpdateUnlocked();
		onComplete = (Action)Delegate.Combine(onComplete, new Action(UpdateUnlocked));
		foreach (TT_Base obj in parents)
		{
			obj.onComplete = (Action)Delegate.Combine(obj.onComplete, new Action(UpdateUnlocked));
		}
	}

	protected void UpdateGrayOut()
	{
		if (elementColors == null)
		{
			elementColors = grayOut.Select(delegate(Image element)
			{
				float num2 = element.color.r * 0.3f + element.color.g * 0.59f + element.color.b * 0.11f;
				return (new Color(num2, num2, num2, element.color.a), color: element.color);
			}).ToArray();
		}
		for (int num = 0; num < grayOut.Count; num++)
		{
			grayOut[num].color = (Data.GrayOut ? elementColors[num].Item1 : elementColors[num].Item2);
		}
	}

	private void UpdateUnlocked()
	{
		if (locked != null)
		{
			locked.gameObject.SetActive(!Data.IsUnlocked());
		}
		UpdateGrayOut();
		OnParentsComplete();
		element.SetEnabled(Data.IsUnlocked());
	}

	public virtual void OnSelect()
	{
	}

	protected virtual void OnParentsComplete()
	{
	}

	protected void OnComplete()
	{
		onComplete?.Invoke();
		HubManager.main.UpdateCareerProgressText();
	}
}
