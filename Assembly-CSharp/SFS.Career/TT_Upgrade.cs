using System;
using SFS.UI;
using UnityEngine.UI;

namespace SFS.Career;

public class TT_Upgrade : TT_Base
{
	public Text title;

	public Text description;

	public Text costText;

	public Text costTextSelected;

	public SFS.UI.Button buyButton;

	private TT_UpgradeData data;

	private string CostText
	{
		get
		{
			if (!(Math.Abs(data.cost) > 0.001))
			{
				return "-";
			}
			return data.cost.ToFundsString();
		}
	}

	public override I_TechTreeData Data => data;

	public void Initialize(TT_UpgradeData data)
	{
		this.data = data;
		description.text = data.description;
		costTextSelected.text = CostText;
		buyButton.onClick += new Action(Buy);
		UpdateUI();
	}

	private void Buy()
	{
		if (data.IsUnlocked() && !data.IsComplete)
		{
			CareerState.main.TryBuy(data.cost, delegate
			{
				CareerState.main.UnlockUpgrade(data.CodeName);
				owner.ToggleSelect(this, 0.1f);
				UpdateUI();
				OnComplete();
			});
		}
	}

	private void UpdateUI()
	{
		title.text = ((!data.IsUnlocked()) ? "??" : data.title);
		costText.text = ((!data.IsUnlocked()) ? "" : CostText);
		buyButton.SetEnabled(data.IsUnlocked() && !data.IsComplete);
	}

	protected override void OnParentsComplete()
	{
		UpdateUI();
	}
}
