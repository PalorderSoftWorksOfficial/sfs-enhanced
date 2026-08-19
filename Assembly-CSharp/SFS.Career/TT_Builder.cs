using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;

namespace SFS.Career;

public class TT_Builder : MonoBehaviour
{
	[Serializable]
	public class TT_Element
	{
		public string name_ID;

		public string parent_Name_ID;

		public Vector2 anchor;

		[NonSerialized]
		public I_TechTreeData asset;

		[NonSerialized]
		public TT_Element parent;

		[NonSerialized]
		public List<TT_Element> children;

		public bool HasParent => parent != null;

		public bool HasChildren => children != null;

		public TT_Element(string name_ID, string parent_Name_ID, Vector2 anchor)
		{
			this.name_ID = name_ID;
			this.parent_Name_ID = parent_Name_ID;
			this.anchor = anchor;
		}

		public void ResetRuntimeData()
		{
			asset = null;
			parent = null;
			children = null;
		}

		public Vector2 GetAnchor(out bool defaultHeight, out bool defaultWidth)
		{
			defaultHeight = anchor.y == -1f;
			defaultWidth = anchor.x == -1f;
			return new Vector2(defaultWidth ? GetDefaultAnchor_X() : anchor.x, defaultHeight ? GetDefaultAnchor_Y() : anchor.y);
		}

		public float GetDefaultAnchor_X()
		{
			return 0f;
		}

		public float GetDefaultAnchor_Y()
		{
			return 80f;
		}

		public void OffsetAnchor(Vector2 offset)
		{
			Vector2 vector = GetAnchor(out var _, out var _) + offset;
			anchor = vector;
			if (anchor.x == GetDefaultAnchor_X())
			{
				anchor.x = -1f;
			}
			if (anchor.y == GetDefaultAnchor_Y())
			{
				anchor.y = -1f;
			}
		}

		public Vector2 GetFromPivot()
		{
			return GetPivot(from: true);
		}

		public Vector2 GetToPivot()
		{
			return GetPivot(from: false);
		}

		public static Vector2 GetPivot(bool from)
		{
			return new Vector2(0.5f, from ? 1 : 0);
		}
	}

	public static TT_Builder main;

	public TT_Creator creator;

	public List<TT_Element> elements = new List<TT_Element>();

	public Dictionary<string, TT_Element> elements_Dictionary;

	private void Awake()
	{
		main = this;
	}

	private void Reload()
	{
		I_TechTreeData[] assets = GetAssets();
		foreach (I_TechTreeData asset in assets)
		{
			if (elements.All((TT_Element element) => element.name_ID != asset.Name_ID))
			{
				elements.Add(new TT_Element(asset.Name_ID, "", Vector2.zero));
			}
		}
		string[] assetNames = (from i_TechTreeData in GetAssets()
			select i_TechTreeData.Name_ID).ToArray();
		elements = elements.Where((TT_Element element) => assetNames.Contains(element.name_ID)).ToList();
		foreach (TT_Element element in elements)
		{
			if (!assetNames.Contains(element.parent_Name_ID))
			{
				element.parent_Name_ID = "";
			}
		}
		SetupRuntimeDictionary();
		TT_Element[] array = elements.Where((TT_Element element) => !element.HasParent && !element.HasChildren).ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].anchor = new Vector2(num % 6 * 600 - 5000, num / 6 * 200);
		}
	}

	public void SetupRuntimeDictionary()
	{
		foreach (TT_Element element in elements)
		{
			element.ResetRuntimeData();
		}
		elements_Dictionary = elements.ToDictionary((TT_Element e) => e.name_ID, (TT_Element e) => e);
		I_TechTreeData[] assets = GetAssets();
		foreach (I_TechTreeData i_TechTreeData in assets)
		{
			if (elements_Dictionary.ContainsKey(i_TechTreeData.Name_ID))
			{
				elements_Dictionary[i_TechTreeData.Name_ID].asset = i_TechTreeData;
			}
		}
		elements_Dictionary = elements_Dictionary.Where((KeyValuePair<string, TT_Element> a) => a.Value.asset != null).ToDictionary((KeyValuePair<string, TT_Element> pair) => pair.Key, (KeyValuePair<string, TT_Element> pair) => pair.Value);
		foreach (TT_Element element2 in elements)
		{
			element2.parent = (elements_Dictionary.TryGetValue(element2.parent_Name_ID, out var value) ? value : null);
		}
		foreach (TT_Element element3 in elements)
		{
			if (element3.HasParent)
			{
				TT_Element tT_Element = elements_Dictionary[element3.parent_Name_ID];
				if (tT_Element.children == null)
				{
					tT_Element.children = new List<TT_Element>();
				}
				tT_Element.children.Add(element3);
			}
		}
	}

	private static I_TechTreeData[] GetAssets()
	{
		List<I_TechTreeData> list = new List<I_TechTreeData>();
		list.AddRange(((Base.partsLoader != null) ? Base.partsLoader.partVariants.Values : PartsLoader.LoadPartVariants().loadedVariants.Values).Where((VariantRef v) => v.GetVariant().cost > 0.0));
		list.AddRange(ResourcesLoader.GetFiles_Array<TT_PartPackData>(""));
		list.AddRange(ResourcesLoader.GetFiles_Array<TT_UpgradeData>(""));
		list.AddRange(ResourcesLoader.GetFiles_Array<TT_InfoData>(""));
		I_TechTreeData[] array = list.ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			if (!(array[num] is TT_PartPackData { parts: var parts }))
			{
				continue;
			}
			foreach (VariantRef variantRef in parts)
			{
				for (int num3 = list.Count - 1; num3 >= 0; num3--)
				{
					if (list[num3].Name_ID == variantRef.GetNameID())
					{
						list.RemoveAt(num3);
					}
				}
			}
		}
		return list.ToArray();
	}
}
