using System;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Career;

public class TT_Parts : TT_Base
{
	public RawImage partPreview;

	public Text costTextSelected;

	public StatsMenu statsMenu;

	public SFS.UI.Button buyButton;

	private I_TechTreeData data;

	private string displayName;

	private double cost;

	private string CostText => cost.ToFundsString();

	public override I_TechTreeData Data => data;

	public void Initialize(VariantRef[] parts, bool centerParts, string displayName, double cost, I_TechTreeData data)
	{
		this.displayName = displayName;
		this.cost = cost;
		this.data = data;
		partPreview.texture = PartIconCreator.main.CreatePartIcon_TechTree(parts, 260, centerParts, 0.5f + (float)parts.Length * 0.5f);
		partPreview.rectTransform.sizeDelta = new Vector2((float)partPreview.texture.width / 2f, (float)partPreview.texture.height / 2f);
		costTextSelected.text = CostText;
		if (data is VariantRef variant)
		{
			Part part = PartsLoader.CreatePart(variant, updateAdaptation: false);
			statsMenu.Open(() => true, delegate(StatsMenu d)
			{
				part.DrawPartStats(new Part[1] { part }, d, PartDrawSettings.PickGridSettings);
			}, skipAnimation: false);
			part.DestroyPart(createExplosion: false, updateJoints: false, DestructionReason.Intentional);
		}
		else
		{
			TT_PartPackData b = data as TT_PartPackData;
			if ((object)b != null)
			{
				statsMenu.Open(() => true, delegate(StatsMenu d)
				{
					d.DrawTitle(b.displayName);
				}, skipAnimation: false);
			}
		}
		grayOut.AddRange(statsMenu.GetComponentsInChildren<Image>());
		buyButton.onClick += new Action(Buy);
		UpdateUI();
	}

	private void Buy()
	{
		if (!data.IsUnlocked() || data.IsComplete)
		{
			return;
		}
		CareerState.main.TryBuy(cost, delegate
		{
			if (data is VariantRef part)
			{
				CareerState.main.UnlockPart(part);
			}
			else if (data is TT_PartPackData)
			{
				CareerState.main.UnlockUpgrade(data.Name_ID);
			}
			owner.ToggleSelect(this, 0.1f);
			UpdateUI();
			OnComplete();
		});
	}

	private void UpdateUI()
	{
		partPreview.color = ((!data.IsUnlocked()) ? new Color(0f, 0f, 0f, 0.25f) : Color.white);
		buyButton.SetEnabled(data.IsUnlocked() && !data.IsComplete);
	}

	protected override void OnParentsComplete()
	{
		UpdateUI();
	}
}
