using System.Collections.Generic;
using SFS.Stats;
using SFS.World;

namespace SFS.Logs;

public class MultiStep : ChallengeStep
{
	public List<ChallengeStep> steps;

	public override bool IsCompleted(Location location, StatsRecorder recorder, ref string progressData)
	{
		for (int i = 0; i < steps.Count; i++)
		{
			if (progressData != null && progressData[i] == '1')
			{
				continue;
			}
			string progressData2 = "";
			if (!steps[i].IsCompleted(location, recorder, ref progressData2))
			{
				continue;
			}
			if (progressData == null)
			{
				progressData = "";
				for (int j = 0; j < steps.Count; j++)
				{
					progressData += "0";
				}
			}
			char[] array = progressData.ToCharArray();
			array[i] = '1';
			progressData = new string(array);
		}
		if (progressData == null)
		{
			return false;
		}
		for (int k = 0; k < steps.Count; k++)
		{
			if (progressData[k] == '0')
			{
				return false;
			}
		}
		return true;
	}

	public override string OnConflict(string a, string b)
	{
		if (a == null || b == null)
		{
			return b ?? a ?? "";
		}
		char[] array = a.ToCharArray();
		for (int i = 0; i < steps.Count; i++)
		{
			if (b[i] == '1')
			{
				array[i] = '1';
			}
		}
		return new string(array);
	}
}
