using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Builds;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.Analytics;

namespace SFS.Career;

public class CareerState : MonoBehaviour
{
	public static CareerState main;

	public WorldSave.CareerState state { get; private set; }

	private bool CompletedTechTree
	{
		get
		{
			if (HasFeature(WorldSave.CareerState.completedTree_1))
			{
				return HasFeature(WorldSave.CareerState.completedTree_2);
			}
			return false;
		}
	}

	public event Action OnFundsChange;

	public event Action OnStateLoaded;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		if (BuildManager.main != null)
		{
			SetState(SavingCache.main.LoadWorldPersistent(MsgDrawer.main, needsRocketsAndBranches: false, eraseCache: false).career);
		}
	}

	public void SetState(WorldSave.CareerState state)
	{
		this.state = state;
		OnStateLoaded?.Invoke();
	}

	public bool HasPart(VariantRef a)
	{
		return HasPart(a.part);
	}

	public bool HasPart(Part part)
	{
		if (part.GetModules<AllowPerMode>().Any((AllowPerMode allow) => !allow.Allowed))
		{
			return false;
		}
		if (CompletedTechTree)
		{
			return true;
		}
		if (ResourcesLoader.main.partPacks["Basics"].parts.Any((VariantRef variantRef) => variantRef.part.name == part.name))
		{
			return true;
		}
		if (part.name == "CapsuleNew")
		{
			return true;
		}
		if (state.unlocked_Parts.Any((string a) => Base.partsLoader.partVariants[a].part.name == part.name))
		{
			return true;
		}
		foreach (string unlocked_Upgrade in state.unlocked_Upgrades)
		{
			if (ResourcesLoader.main.partPacks.TryGetValue(unlocked_Upgrade, out var value) && value.parts.Any((VariantRef variantRef) => variantRef.part.name == part.name))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasUpgrade(string name_ID)
	{
		if (Base.worldBase.IsCareer)
		{
			return state.unlocked_Upgrades.Contains(name_ID);
		}
		return true;
	}

	public bool HasFeature((WorldSave.CareerState.UnlockType type, string id) a)
	{
		if (!Base.worldBase.IsCareer)
		{
			return true;
		}
		return a.type switch
		{
			WorldSave.CareerState.UnlockType.Part => state.unlocked_Parts.Contains(a.id), 
			WorldSave.CareerState.UnlockType.Upgrade => state.unlocked_Upgrades.Contains(a.id), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public void UnlockPart(VariantRef part)
	{
		AddToList(state.unlocked_Parts, part.GetNameID());
		AnalyticsEvent.Custom("Unlock_" + part.GetNameID(), new Dictionary<string, object>());
	}

	public void UnlockUpgrade(string upgrade_CodeName)
	{
		AddToList(state.unlocked_Upgrades, upgrade_CodeName);
		AnalyticsEvent.Custom("Unlock_" + upgrade_CodeName, new Dictionary<string, object>());
	}

	private static void AddToList(List<string> unlockedList, string codeName)
	{
		if (!unlockedList.Contains(codeName))
		{
			unlockedList.Add(codeName);
		}
	}

	public void TryBuy(double cost, Action onBuy)
	{
		if (!CanBuy(cost))
		{
			MsgDrawer.main.Log("Insufficient funds");
			SoundPlayer.main.denySound.Play();
		}
		else
		{
			TakeFunds(cost);
			onBuy?.Invoke();
		}
	}

	private bool CanBuy(double cost)
	{
		return cost <= state.funds;
	}

	public void RewardFunds(double a)
	{
		state.funds += a;
		OnFundsChange?.Invoke();
	}

	private void TakeFunds(double a)
	{
		state.funds -= a;
		OnFundsChange?.Invoke();
	}
}
