using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class ResourceDrawer : MonoBehaviour
{
	public interface I_Resource
	{
		ResourceType ResourceType { get; }

		float WetMass { get; }

		Double_Reference ResourcePercent { get; }
	}

	public Transform resourcesHolder;

	public RectTransform textPrefab;

	public ResourceBar barPrefab;

	public RectTransform temperatureMenuAttachPoint;

	public GameObject temperatureHolder;

	public GameObject combinedMenuHolder;

	public Button plantFlagButton;

	private Local_GenericTarget target = new Local_GenericTarget();

	private Pool<TextAdapter> resourceNames;

	private Pool<ResourceBar> resourceBars;

	private Action onResetElements;

	private Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>> currentState = new Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>>();

	private void Start()
	{
		combinedMenuHolder.gameObject.SetActive(value: false);
		textPrefab.gameObject.SetActive(value: false);
		barPrefab.gameObject.SetActive(value: false);
		resourceNames = new Pool<TextAdapter>(() => UnityEngine.Object.Instantiate(textPrefab, resourcesHolder).GetComponentInChildren<TextAdapter>(), delegate(TextAdapter text)
		{
			text.transform.parent.gameObject.SetActive(value: false);
		});
		resourceBars = new Pool<ResourceBar>(() => UnityEngine.Object.Instantiate(barPrefab, resourcesHolder), delegate(ResourceBar bar)
		{
			bar.gameObject.SetActive(value: false);
		});
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
		WorldTime.main.realtimePhysics.OnChange += new Action(OnPlayerChange);
		target.OnChange += new Action<ObservableMonoBehaviour, ObservableMonoBehaviour>(OnResourcesChange);
	}

	private void OnDestroy()
	{
		PlayerController.main.player.OnChange -= new Action(OnPlayerChange);
		WorldTime.main.realtimePhysics.OnChange -= new Action(OnPlayerChange);
		target.OnChange -= new Action<ObservableMonoBehaviour, ObservableMonoBehaviour>(OnResourcesChange);
		onResetElements?.Invoke();
	}

	private void OnPlayerChange()
	{
		target.Value = ((PlayerController.main.player.Value is Rocket rocket) ? ((ObservableMonoBehaviour)rocket.resources) : ((ObservableMonoBehaviour)(PlayerController.main.player.Value as Astronaut_EVA)));
	}

	private void OnResourcesChange(ObservableMonoBehaviour valueOld, ObservableMonoBehaviour valueNew)
	{
		if (valueOld is Resources resources)
		{
			resources.onGroupsSetup = (Action)Delegate.Remove(resources.onGroupsSetup, new Action(ForceRedraw));
		}
		resourcesHolder.gameObject.SetActive(valueNew != null);
		base.enabled = valueNew != null;
		_ = base.enabled;
		if (valueNew is Resources resources2)
		{
			resources2.onGroupsSetup = (Action)Delegate.Combine(resources2.onGroupsSetup, new Action(ForceRedraw));
			CheckRedraw(forceRedraw: true);
		}
		if (valueNew is Astronaut_EVA astronaut_EVA)
		{
			ResetDraw();
			EVA_Resources r = astronaut_EVA.resources;
			DrawSingleResource(() => Loc.main.Resource_Bars_Title.InjectField(Loc.main.Astronaut_Fuel, "resource_name"), delegate(Action<double> a)
			{
				r.fuelPercent.OnChange += a;
			}, delegate(Action<double> a)
			{
				r.fuelPercent.OnChange -= a;
			});
			PositionTemperatureHolder(hasResources: true);
		}
	}

	private void Update()
	{
		combinedMenuHolder.SetActive(temperatureHolder.activeSelf || resourcesHolder.gameObject.activeSelf);
	}

	private void LateUpdate()
	{
		CheckRedraw(forceRedraw: false);
	}

	private void ForceRedraw()
	{
		CheckRedraw(forceRedraw: true);
	}

	private void CheckRedraw(bool forceRedraw)
	{
		Resources resources = target.Value as Resources;
		if (resources == null)
		{
			return;
		}
		bool stackResourceBarsAtAll = resources.localGroups.Length > 6;
		int counter = 0;
		Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>> newState = new Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>>();
		_ = WorldTime.main.realtimePhysics.Value;
		ResourceModule[] localGroups = resources.localGroups;
		foreach (ResourceModule module in localGroups)
		{
			A(module);
		}
		BoosterModule[] boosters = resources.boosters;
		foreach (BoosterModule module2 in boosters)
		{
			A(module2);
		}
		if (newState.Sum((KeyValuePair<ResourceType, Dictionary<(int, int), (I_Resource, int)>> a) => a.Value.Count) > 6)
		{
			Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>> dictionary = new Dictionary<ResourceType, Dictionary<(int, int), (I_Resource, int)>>();
			foreach (KeyValuePair<ResourceType, Dictionary<(int, int), (I_Resource, int)>> item3 in newState)
			{
				ResourceType key = item3.Key;
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, new Dictionary<(int, int), (I_Resource, int)>());
				}
				foreach (KeyValuePair<(int, int), (I_Resource, int)> item4 in item3.Value)
				{
					(int, int) key2 = (item4.Key.Item1, 0);
					if (!dictionary[key].ContainsKey(key2))
					{
						dictionary[key].Add(key2, (item4.Value.Item1, 0));
					}
					dictionary[key][key2] = (item4.Value.Item1, dictionary[key][key2].Item2 + 1);
				}
			}
			newState = dictionary;
		}
		if (NeedsRedraw())
		{
			CompleteDraw();
		}
		void A(I_Resource i_Resource)
		{
			(int, int) key3 = (stackResourceBarsAtAll ? ((int)Math.Round(i_Resource.ResourcePercent.Value * 10000.0)) : counter++, (int)Math.Round(i_Resource.WetMass * 10000f));
			if (!newState.ContainsKey(i_Resource.ResourceType))
			{
				newState.Add(i_Resource.ResourceType, new Dictionary<(int, int), (I_Resource, int)>());
			}
			if (!newState[i_Resource.ResourceType].ContainsKey(key3))
			{
				newState[i_Resource.ResourceType].Add(key3, (i_Resource, 0));
			}
			newState[i_Resource.ResourceType][key3] = (i_Resource, newState[i_Resource.ResourceType][key3].Item2 + 1);
		}
		void CompleteDraw()
		{
			ResetDraw();
			currentState = newState;
			bool flag = false;
			CrewModule[] modules = resources.GetComponentInParent<Rocket>().partHolder.GetModules<CrewModule>();
			for (int j = 0; j < modules.Length; j++)
			{
				CrewModule.Seat[] seats = modules[j].seats;
				foreach (CrewModule.Seat seat in seats)
				{
					bool ready;
					if (seat.externalSeat && seat.HasAstronaut)
					{
						flag = true;
						EVA_Resources r = seat.resources;
						DrawSingleResource(() => Loc.main.Resource_Bars_Title.InjectField(Loc.main.Astronaut_Fuel, "resource_name"), delegate(Action<double> a)
						{
							r.fuelPercent.OnChange += a;
						}, delegate(Action<double> a)
						{
							r.fuelPercent.OnChange -= a;
						});
						ready = false;
						seat.astronaut.OnChange += new Action(Redraw);
						ready = true;
						onResetElements = (Action)Delegate.Combine(onResetElements, (Action)delegate
						{
							seat.astronaut.OnChange -= new Action(Redraw);
						});
					}
					void Redraw()
					{
						if (ready)
						{
							ForceRedraw();
						}
					}
				}
			}
			if (flag && newState.Count > 0)
			{
				TextAdapter item = resourceNames.GetItem();
				Transform parent = item.transform.parent;
				parent.gameObject.SetActive(value: true);
				parent.SetAsLastSibling();
				item.Text = "";
			}
			foreach (KeyValuePair<ResourceType, Dictionary<(int, int), (I_Resource, int)>> item5 in newState)
			{
				KeyValuePair<ResourceType, Dictionary<(int, int), (I_Resource, int)>> section = item5;
				TextAdapter text = resourceNames.GetItem();
				Transform parent2 = text.transform.parent;
				parent2.gameObject.SetActive(value: true);
				parent2.SetAsLastSibling();
				Loc.OnChange += new Action(UpdateTitle);
				onResetElements = (Action)Delegate.Combine(onResetElements, (Action)delegate
				{
					Loc.OnChange -= new Action(UpdateTitle);
				});
				foreach (var value in section.Value.Values)
				{
					I_Resource resourceModule = value.Item1;
					int item2 = value.Item2;
					ResourceBar barUI = resourceBars.GetItem();
					barUI.gameObject.SetActive(value: true);
					barUI.transform.SetAsLastSibling();
					barUI.countText.Text = ((item2 > 1) ? (item2 + "x") : "");
					resourceModule.ResourcePercent.OnChange += new Action<double>(OnChange);
					onResetElements = (Action)Delegate.Combine(onResetElements, (Action)delegate
					{
						resourceModule.ResourcePercent.OnChange -= new Action<double>(OnChange);
					});
					void OnChange(double fillAmount)
					{
						barUI.UpdatePercent((float)fillAmount);
					}
				}
				void UpdateTitle()
				{
					text.Text = Loc.main.Resource_Bars_Title.InjectField(section.Key.displayName.Field, "resource_name");
				}
			}
			PositionTemperatureHolder((newState.Count > 0) | flag);
		}
		bool NeedsRedraw()
		{
			if (forceRedraw || !newState.Keys.SequenceEqual(currentState.Keys))
			{
				return true;
			}
			for (int j = 0; j < newState.Count; j++)
			{
				if (currentState.Count != newState.Count)
				{
					return true;
				}
				Dictionary<(int, int), (I_Resource, int)> dictionary2 = currentState.Values.ToArray()[j];
				Dictionary<(int, int), (I_Resource, int)> dictionary3 = newState.Values.ToArray()[j];
				for (int k = 0; k < dictionary3.Count; k++)
				{
					if (dictionary2.Values.ToArray()[k].Item2 != dictionary3.Values.ToArray()[k].Item2)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	private void DrawSingleResource(Func<string> resourceName, Action<Action<double>> register, Action<Action<double>> unregister)
	{
		TextAdapter text = resourceNames.GetItem();
		Transform parent = text.transform.parent;
		parent.gameObject.SetActive(value: true);
		parent.SetAsLastSibling();
		Loc.OnChange += new Action(UpdateTitle);
		onResetElements = (Action)Delegate.Combine(onResetElements, (Action)delegate
		{
			Loc.OnChange -= new Action(UpdateTitle);
		});
		ResourceBar barUI = resourceBars.GetItem();
		barUI.gameObject.SetActive(value: true);
		barUI.transform.SetAsLastSibling();
		barUI.countText.Text = "";
		register(OnChange);
		onResetElements = (Action)Delegate.Combine(onResetElements, (Action)delegate
		{
			unregister(OnChange);
		});
		void OnChange(double fillAmount)
		{
			barUI.UpdatePercent((float)fillAmount);
		}
		void UpdateTitle()
		{
			text.Text = resourceName();
		}
	}

	private void ResetDraw()
	{
		resourceBars.Reset();
		resourceNames.Reset();
		onResetElements?.Invoke();
		onResetElements = null;
	}

	private void PositionTemperatureHolder(bool hasResources)
	{
		resourcesHolder.gameObject.SetActive(hasResources);
	}
}
