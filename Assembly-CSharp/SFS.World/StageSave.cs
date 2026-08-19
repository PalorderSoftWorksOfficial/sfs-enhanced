using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Parts;

namespace SFS.World;

[Serializable]
public class StageSave
{
	public int stageId;

	public int[] partIndexes;

	public static StageSave[] CreateSaves(Staging staging, List<Part> parts)
	{
		return staging.stages.Select(delegate(Stage stage)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < stage.PartCount; i++)
			{
				int num = parts.IndexOf(stage.parts[i]);
				if (num != -1)
				{
					list.Add(num);
				}
			}
			return new StageSave
			{
				stageId = stage.stageId,
				partIndexes = list.ToArray()
			};
		}).ToArray();
	}
}
