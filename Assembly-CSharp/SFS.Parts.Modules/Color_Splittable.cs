using System.Collections.Generic;
using System.Linq;
using UV;

namespace SFS.Parts.Modules;

public class Color_Splittable : Splittable
{
	public Color_Channel element;

	public Color_Splittable(Color_Channel element)
	{
		this.element = element;
	}

	protected override float[] GetSplits()
	{
		if (element.elements.Count == 0)
		{
			return new float[0];
		}
		List<float> list = new List<float>();
		list.Add(element.elements[0].height.start);
		list.AddRange(element.elements.Select((StartEnd_Color UV) => UV.height.end));
		return list.ToArray();
	}

	protected override void Split(float[] splits)
	{
		if (element.elements.Count == 0)
		{
			return;
		}
		List<StartEnd_Color> list = new List<StartEnd_Color>();
		int num = 0;
		for (int i = 0; i < splits.Length - 1; i++)
		{
			if (splits[i] >= element.elements[num].height.end && num < element.elements.Count - 1)
			{
				num++;
			}
			Line cut = new Line(splits[i], splits[i + 1]);
			list.Add(element.elements[num].Cut(cut));
		}
		element.elements = list;
	}
}
