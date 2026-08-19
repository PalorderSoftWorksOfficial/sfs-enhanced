using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Logs;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using UnityEngine;

namespace SFS.Stats;

public class ChallengeRecorder
{
	private List<(Challenge, (int, string))> eligibleSteps = new List<(Challenge, (int, string))>();

	private Dictionary<Challenge, (int i, string progressData)> progress;

	private HashSet<Challenge> complete;

	private StatsRecorder owner;

	public ChallengeRecorder(Dictionary<Challenge, (int i, string progressData)> progress, HashSet<Challenge> complete, StatsRecorder owner)
	{
		this.progress = progress;
		this.complete = complete;
		this.owner = owner;
		UpdateEligibleSteps();
	}

	public void UpdateEligibleSteps()
	{
		eligibleSteps.Clear();
		Challenge[] challengesArray = Base.worldBase.challengesArray;
		foreach (Challenge challenge in challengesArray)
		{
			if (LogManager.main.completeChallenges.Contains(challenge.id))
			{
				continue;
			}
			if (challenge.steps.Count == 0)
			{
				Debug.LogError("No mission steps: " + challenge.title());
			}
			else if (!complete.Contains(challenge))
			{
				int num;
				string item;
				if (!progress.TryGetValue(challenge, out (int, string) value))
				{
					num = 0;
					item = null;
				}
				else if (value.Item2 != null)
				{
					(num, item) = value;
				}
				else
				{
					num = value.Item1 + 1;
					item = null;
				}
				if (IsEligible(challenge, num))
				{
					eligibleSteps.Add((challenge, (num, item)));
				}
			}
		}
	}

	private bool IsEligible(Challenge a, int index)
	{
		return a.steps[index].IsEligible(owner.location.planet.Value);
	}

	public void TryCompleteSteps(Location location)
	{
		Action after = null;
		foreach (var eligibleStep in eligibleSteps)
		{
			(int, string) item = eligibleStep.Item2;
			Challenge challenge = eligibleStep.Item1;
			int index = item.Item1;
			string progressData = item.Item2;
			string progressData_New = progressData;
			bool shownCompleteMsg = challenge.steps[index].IsCompleted(location, owner, ref progressData_New);
			if (!shownCompleteMsg && progressData_New != null && progressData_New != progressData)
			{
				progress[challenge] = (index, progressData_New);
				LogManager.main.branches[owner.branch].challengeEvents.Add(challenge.id + "," + index + "," + progressData_New);
				after = delegate
				{
					eligibleSteps.Remove((challenge, (index, progressData)));
					eligibleSteps.Add((challenge, (index, progressData_New)));
				};
			}
			if (shownCompleteMsg)
			{
				CompleteStep(challenge, index, progressData, progressData_New, ref after, out shownCompleteMsg);
				break;
			}
		}
		after?.Invoke();
	}

	public void OnCrash(float impactVelocity)
	{
		Action after = null;
		foreach (var (challenge, (index, progressData)) in eligibleSteps)
		{
			if (challenge.steps[index] is Step_Impact step_Impact && impactVelocity > (float)step_Impact.impactVelocity)
			{
				CompleteStep(challenge, index, progressData, null, ref after, out var _);
			}
		}
		after?.Invoke();
	}

	private void CompleteStep(Challenge challenge, int index, string progressData, string progressData_New, ref Action after, out bool shownCompleteMsg)
	{
		shownCompleteMsg = false;
		after = (Action)Delegate.Combine(after, (Action)delegate
		{
			eligibleSteps.Remove((challenge, (index, progressData)));
			eligibleSteps.Remove((challenge, (index, progressData_New)));
		});
		if (index == challenge.steps.Count - 1)
		{
			progress.Remove(challenge);
			complete.Add(challenge);
			LogManager.main.branches[owner.branch].challengeEvents.Add(challenge.id + "," + index);
			if ((bool)owner.player.isPlayer)
			{
				MsgDrawer.main.Log(Loc.main.Challenges_Completed.Inject(challenge.title(), "challenge"), big: true);
				shownCompleteMsg = true;
			}
			if (!challenge.returnSafely && !LogManager.main.completeChallenges.Contains(challenge.id))
			{
				LogManager.main.completeChallenges.Add(challenge.id);
			}
			return;
		}
		progress[challenge] = (index, null);
		LogManager.main.branches[owner.branch].challengeEvents.Add(challenge.id + "," + index);
		after = (Action)Delegate.Combine(after, (Action)delegate
		{
			if (IsEligible(challenge, index + 1))
			{
				eligibleSteps.Add((challenge, (index + 1, null)));
			}
		});
	}

	public void Merge(ChallengeRecorder b)
	{
		foreach (KeyValuePair<Challenge, (int, string)> item2 in b.progress)
		{
			Challenge key = item2.Key;
			if (progress.TryGetValue(key, out (int, string) value))
			{
				if (item2.Value.Item1 > value.Item1)
				{
					progress[key] = item2.Value;
				}
				else if (item2.Value.Item1 == value.Item1)
				{
					string item = key.steps[value.Item1].OnConflict(value.Item2, item2.Value.Item2);
					progress[key] = (value.Item1, item);
				}
			}
			else
			{
				progress.Add(key, item2.Value);
			}
		}
		foreach (Challenge item3 in b.complete)
		{
			if (!complete.Contains(item3))
			{
				complete.Add(item3);
			}
		}
	}

	public void Split(out Dictionary<Challenge, (int i, string progressData)> progress, out HashSet<Challenge> complete)
	{
		progress = this.progress.ToDictionary((KeyValuePair<Challenge, (int i, string progressData)> a) => a.Key, (KeyValuePair<Challenge, (int i, string progressData)> a) => a.Value);
		complete = this.complete.ToHashSet();
	}

	public HashSet<Challenge> GetCompleteChallenges()
	{
		return complete;
	}
}
