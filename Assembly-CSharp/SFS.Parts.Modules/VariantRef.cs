using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Builds;
using SFS.Career;
using UnityEngine;

namespace SFS.Parts.Modules;

[Serializable]
public class VariantRef : I_TechTreeData
{
	public Part part;

	[HideInInspector]
	public int variantIndex_A = -1;

	[HideInInspector]
	public int variantIndex_B = -1;

	private string Variant
	{
		get
		{
			return GetLabel(variantIndex_A, variantIndex_B);
		}
		set
		{
			variantIndex_A = int.Parse(value.Split(',', StringSplitOptions.None)[0]);
			variantIndex_B = int.Parse(value.Split(',', StringSplitOptions.None)[1]);
		}
	}

	bool I_TechTreeData.IsComplete => CareerState.main.HasPart(this);

	bool I_TechTreeData.GrayOut => !((I_TechTreeData)this).IsComplete;

	string I_TechTreeData.Name_ID => GetNameID();

	public int Value => 1;

	private Part[] PartOptions()
	{
		return PartsLoader.LoadParts().Values.ToArray();
	}

	private string[] Variants()
	{
		if (part == null)
		{
			return new string[1] { -1 + ", " + -1 };
		}
		List<string> list = new List<string>();
		for (int i = 0; i < part.variants.Length; i++)
		{
			for (int j = 0; j < part.variants[i].variants.Length; j++)
			{
				list.Add(GetLabel(i, j));
			}
		}
		return list.ToArray();
	}

	private string GetLabel(int a, int b)
	{
		string text = a + ", " + b;
		if (part != null && part.variants.IsValidIndex(a) && part.variants[a].variants.IsValidIndex(b))
		{
			Variants.Variable[] changes = part.variants[a].variants[b].changes;
			foreach (Variants.Variable variable in changes)
			{
				text = text + ", " + variable.GetLabel();
			}
		}
		return text;
	}

	public Variants.Variant GetVariant()
	{
		return part.variants[variantIndex_A].variants[variantIndex_B];
	}

	public string GetNameID()
	{
		return part.name + "_" + variantIndex_A + "_" + variantIndex_B;
	}

	public VariantRef(Part part, int variantIndex_A, int variantIndex_B)
	{
		this.part = part;
		this.variantIndex_A = variantIndex_A;
		this.variantIndex_B = variantIndex_B;
	}

	public List<Variants.PickTag> GetPickTags()
	{
		if (variantIndex_A == -1)
		{
			return new List<Variants.PickTag>();
		}
		List<Variants.PickTag> list = new List<Variants.PickTag>();
		if (part.variants[variantIndex_A].tags != null)
		{
			Variants.PickTag[] tags = part.variants[variantIndex_A].tags;
			foreach (Variants.PickTag item in tags)
			{
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		if (GetVariant().tags != null)
		{
			Variants.PickTag[] tags = GetVariant().tags;
			foreach (Variants.PickTag item2 in tags)
			{
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	public int GetPriority(PickCategory tag)
	{
		Variants.PickTag[] tags = part.variants[variantIndex_A].tags;
		foreach (Variants.PickTag pickTag in tags)
		{
			if (pickTag.tag == tag)
			{
				return pickTag.priority;
			}
		}
		tags = GetVariant().tags;
		foreach (Variants.PickTag pickTag2 in tags)
		{
			if (pickTag2.tag == tag)
			{
				return pickTag2.priority;
			}
		}
		throw new Exception("Tag not found");
	}
}
