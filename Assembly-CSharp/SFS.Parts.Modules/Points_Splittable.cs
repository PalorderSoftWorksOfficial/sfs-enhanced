using System.Collections.Generic;
using System.Linq;

namespace SFS.Parts.Modules;

public class Points_Splittable : Splittable
{
	public List<PipePoint> elements;

	public Points_Splittable(List<PipePoint> elements)
	{
		this.elements = elements;
	}

	public Line GetSegment()
	{
		return new Line(0f, elements.Last().height);
	}

	protected override float[] GetSplits()
	{
		return elements.Select((PipePoint x) => x.height).ToArray();
	}

	protected override void Split(float[] splits)
	{
		if (elements.Count == 0)
		{
			return;
		}
		List<PipePoint> list = new List<PipePoint>();
		int num = 0;
		foreach (float num2 in splits)
		{
			if (num2 >= elements[num + 1].height && num < elements.Count - 2)
			{
				num++;
			}
			PipePoint a = elements[num];
			PipePoint b = elements[num + 1];
			list.Add(PipePoint.LerpByHeight(a, b, num2));
		}
		elements = list;
	}
}
