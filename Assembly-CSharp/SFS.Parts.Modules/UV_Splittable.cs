using System.Collections.Generic;
using System.Linq;
using UV;

namespace SFS.Parts.Modules;

public class UV_Splittable : Splittable
{
	public List<StartEnd_UV> element;

	public UV_Splittable(List<StartEnd_UV> element)
	{
		this.element = element;
	}

	protected override float[] GetSplits()
	{
		if (element.Count == 0)
		{
			return new float[0];
		}
		List<float> list = new List<float>();
		list.Add(element[0].height.start);
		list.AddRange(element.Select((StartEnd_UV uv) => uv.height.end));
		return list.ToArray();
	}

	protected override void Split(float[] splits)
	{
		if (element.Count == 0)
		{
			return;
		}
		List<StartEnd_UV> list = new List<StartEnd_UV>();
		int num = 0;
		for (int i = 0; i < splits.Length - 1; i++)
		{
			if (splits[i] >= element[num].height.end && num < element.Count - 1)
			{
				num++;
			}
			Line cut = new Line(splits[i], splits[i + 1]);
			list.Add(element[num].Cut(cut));
		}
		element = list;
	}
}
