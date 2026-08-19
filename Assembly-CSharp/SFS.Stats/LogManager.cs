using System.Collections.Generic;
using System.Linq;
using SFS.Logs;
using SFS.World;
using UnityEngine;

namespace SFS.Stats;

public class LogManager : MonoBehaviour
{
	public static LogManager main;

	public Dictionary<int, Branch> branches = new Dictionary<int, Branch>();

	public HashSet<LogId> completeLogs = new HashSet<LogId>();

	public HashSet<string> completeChallenges = new HashSet<string>();

	private void Awake()
	{
		main = this;
	}

	public void ClearBranches()
	{
		List<int> list = new List<int>();
		foreach (Rocket rocket in GameManager.main.rockets)
		{
			list.Add(rocket.stats.branch);
		}
		foreach (Astronaut_EVA item in AstronautManager.main.eva)
		{
			list.Add(item.stats.branch);
		}
		ClearBranches(list);
	}

	private void ClearBranches(List<int> startBranches)
	{
		HashSet<int> traversed = new HashSet<int>();
		foreach (int startBranch in startBranches)
		{
			CollectParentBranches(startBranch);
		}
		int[] array = branches.Keys.ToArray();
		foreach (int num in array)
		{
			if (!traversed.Contains(num))
			{
				branches.Remove(num);
			}
		}
		void CollectParentBranches(int branch)
		{
			if (branches.ContainsKey(branch) && !traversed.Contains(branch))
			{
				traversed.Add(branch);
				Branch branch2 = branches[branch];
				if (branch2.parentA != -1)
				{
					CollectParentBranches(branch2.parentA);
				}
				if (branch2.parentB != -1)
				{
					CollectParentBranches(branch2.parentB);
				}
			}
		}
	}

	public void CreateRoot(out int newBranch)
	{
		CreateBranch(-1, -1, out newBranch);
	}

	public void SplitBranch(int branch, out int newBranch_A, out int newBranch_B)
	{
		CreateBranch(branch, -1, out newBranch_A);
		CreateBranch(branch, -1, out newBranch_B);
	}

	public void MergeBranch(int branch_A, int branch_B, out int newBranch)
	{
		CreateBranch(branch_A, branch_B, out newBranch);
	}

	private void CreateBranch(int parent_A, int parent_B, out int newBranch)
	{
		Branch value = new Branch
		{
			parentA = parent_A,
			parentB = parent_B,
			startTime = WorldTime.main.worldTime
		};
		newBranch = ((branches.Count != 0) ? (branches.Keys.Max() + 1) : 0);
		branches[newBranch] = value;
	}
}
