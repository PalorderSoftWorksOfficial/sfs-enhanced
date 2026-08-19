using System.Collections.Generic;

namespace SFS.Parts.Modules;

public abstract class Splittable
{
	public static void Split(Line segment, params Splittable[] A)
	{
		float[] splits = GetSplits(segment, A);
		for (int i = 0; i < A.Length; i++)
		{
			A[i].Split(splits);
		}
	}

	private static float[] GetSplits(Line cut, Splittable[] A)
	{
		List<float> list = new List<float>();
		for (int i = 0; i < A.Length; i++)
		{
			float[] splits = A[i].GetSplits();
			foreach (float num in splits)
			{
				if (num >= cut.start && num <= cut.end && !list.Contains(num))
				{
					list.Add(num);
				}
			}
		}
		list.Sort();
		return list.ToArray();
	}

	protected abstract float[] GetSplits();

	protected abstract void Split(float[] splits);
}
