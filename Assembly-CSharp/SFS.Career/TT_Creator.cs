using System;
using System.Collections.Generic;
using SFS.Parts.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.Career;

public class TT_Creator : MonoBehaviour
{
	public static TT_Creator main;

	public TT_Parts partsPrefab;

	public TT_Upgrade upgradePrefab;

	public TT_Info infoPrefab;

	public RectTransform linePrefab;

	[Space]
	public Color lockedColor;

	public Color unlockedColor;

	public Color completeColor;

	private void Awake()
	{
		main = this;
	}

	public void CreateTrees(TreeComponent[] trees)
	{
		TT_Builder tT_Builder = TT_Builder.main;
		tT_Builder.SetupRuntimeDictionary();
		Dictionary<TT_Builder.TT_Element, TT_Base> created;
		TT_Builder.TT_Element element;
		foreach (TreeComponent tree in trees)
		{
			TreeComponent.Root[] roots = tree.roots;
			foreach (TreeComponent.Root root in roots)
			{
				List<TT_Builder.TT_Element> used = new List<TT_Builder.TT_Element>();
				CollectChildren(tT_Builder.elements_Dictionary[root.codeName]);
				created = new Dictionary<TT_Builder.TT_Element, TT_Base>();
				foreach (TT_Builder.TT_Element item in used)
				{
					element = item;
					I_TechTreeData asset = element.asset;
					if (!(asset is VariantRef variantRef))
					{
						if (!(asset is TT_PartPackData tT_PartPackData))
						{
							if (!(asset is TT_UpgradeData tT_UpgradeData))
							{
								if (asset is TT_InfoData tT_InfoData)
								{
									Create<TT_Info>(infoPrefab, tT_InfoData.name).Initialize(tT_InfoData);
								}
							}
							else
							{
								Create<TT_Upgrade>(upgradePrefab, tT_UpgradeData.name).Initialize(tT_UpgradeData);
							}
						}
						else
						{
							Create<TT_Parts>(partsPrefab, tT_PartPackData.name).Initialize(tT_PartPackData.parts, tT_PartPackData.center, tT_PartPackData.displayName, tT_PartPackData.cost, tT_PartPackData);
						}
					}
					else
					{
						Create<TT_Parts>(partsPrefab, variantRef.part.name).Initialize(new VariantRef[1] { variantRef }, centerParts: false, variantRef.part.GetDisplayName(), variantRef.GetVariant().cost, variantRef);
					}
				}
				foreach (KeyValuePair<TT_Builder.TT_Element, TT_Base> item2 in created)
				{
					item2.Value.transform.localPosition = GetPosition(item2.Key, applyLastAnchor: false) + root.position;
					item2.Value.SetupParents(tree, (item2.Key.parent != null) ? new TT_Base[1] { created[item2.Key.parent] } : new TT_Base[0]);
				}
				foreach (KeyValuePair<TT_Builder.TT_Element, TT_Base> item3 in created)
				{
					KeyValuePair<TT_Builder.TT_Element, TT_Base> element2 = item3;
					TT_Builder.TT_Element key = element2.Key;
					Vector2[] points;
					if (key.HasParent)
					{
						TT_Builder.TT_Element parent = key.parent;
						Vector2 vector = GetPositionAtPivot(key, key.GetToPivot(), applyLastAnchor: false) + root.position;
						Vector2 vector2 = GetPositionAtPivot(parent, key.GetFromPivot(), applyLastAnchor: false) + root.position;
						if (Mathf.Abs(vector.x - vector2.x) > 7f)
						{
							points = new Vector2[4]
							{
								vector2,
								new Vector2(vector2.x, (vector2.y + vector.y) / 2f),
								new Vector2(vector.x, (vector2.y + vector.y) / 2f),
								vector
							};
							Create2(0, new Vector2(7f, points[1].y - points[0].y + 7f));
							Create2(2, new Vector2(7f, points[3].y - points[2].y + 7f));
							Create2(1, new Vector2(Mathf.Abs(points[2].x - points[1].x) - 7f, 7f));
						}
						else
						{
							points = new Vector2[2] { vector2, vector };
							Create2(0, new Vector2(7f, points[1].y - points[0].y + 7f));
						}
					}
					void Create2(int num, Vector2 size)
					{
						RectTransform line = UnityEngine.Object.Instantiate(linePrefab, tree.treeHolder);
						line.name = "Line";
						line.SetSiblingIndex(0);
						line.localPosition = (points[num] + points[num + 1]) / 2f;
						line.sizeDelta = size;
						UpdateColor();
						TT_Base value = element2.Value;
						value.onComplete = (Action)Delegate.Combine(value.onComplete, new Action(UpdateColor));
						void UpdateColor()
						{
							line.GetComponent<Image>().color = (element2.Value.Data.IsComplete ? completeColor : (element2.Value.Data.IsUnlocked() ? unlockedColor : lockedColor));
						}
					}
				}
				foreach (TT_Base element3 in created.Values)
				{
					element3.element.onClick += (Action)delegate
					{
						if (element3.Data.IsUnlocked())
						{
							tree.ToggleSelect(element3);
						}
					};
					tree.viewScroller.RegisterScrolling(element3.element);
				}
				void CollectChildren(TT_Builder.TT_Element a)
				{
					used.Add(a);
					if (!a.HasChildren)
					{
						return;
					}
					foreach (TT_Builder.TT_Element child in a.children)
					{
						CollectChildren(child);
					}
				}
			}
			T Create<T>(T prefab, string name) where T : TT_Base
			{
				T val = UnityEngine.Object.Instantiate(prefab, tree.treeHolder);
				val.name = name;
				if (val.unselectedHolder != null)
				{
					val.unselectedHolder.SetActive(value: true);
				}
				if (val.selectedHolder != null)
				{
					val.selectedHolder.SetActive(value: false);
				}
				created.Add(element, val);
				return val;
			}
		}
	}

	public static (int countComplete, int countTotal) GetProgress(TreeComponent tree)
	{
		int countComplete = 0;
		int countTotal = 0;
		TreeComponent.Root[] roots = tree.roots;
		foreach (TreeComponent.Root root in roots)
		{
			CollectChildren(TT_Builder.main.elements_Dictionary[root.codeName]);
		}
		return (countComplete: countComplete, countTotal: countTotal);
		void CollectChildren(TT_Builder.TT_Element a)
		{
			int value = a.asset.Value;
			if (a.asset.IsComplete)
			{
				countComplete += value;
			}
			countTotal += value;
			if (a.HasChildren)
			{
				foreach (TT_Builder.TT_Element child in a.children)
				{
					CollectChildren(child);
				}
			}
		}
	}

	public Vector2 GetPositionAtPivot(TT_Builder.TT_Element element, Vector2 pivot, bool applyLastAnchor)
	{
		RectTransform elementRect = GetElementRect(element);
		return GetPosition(element, applyLastAnchor) + elementRect.sizeDelta * (pivot - elementRect.pivot);
	}

	private Vector2 GetPosition(TT_Builder.TT_Element element, bool applyLastAnchor)
	{
		Vector2 result = GetElementRect(element).sizeDelta * (GetElementRect(element).pivot - element.GetToPivot());
		int num = 0;
		bool defaultHeight;
		bool defaultWidth;
		while (element.HasParent)
		{
			if (num > 20)
			{
				Debug.LogWarning("LOOP: " + element.name_ID + "  " + element.parent_Name_ID);
				break;
			}
			result += element.GetAnchor(out defaultHeight, out defaultWidth) + GetElementRect(element.parent).sizeDelta * (element.GetFromPivot() - element.parent.GetToPivot());
			element = element.parent;
			num++;
		}
		if (applyLastAnchor)
		{
			result += element.GetAnchor(out defaultWidth, out defaultHeight);
		}
		return result;
	}

	public RectTransform GetElementRect(TT_Builder.TT_Element element)
	{
		return partsPrefab.GetRect();
	}
}
