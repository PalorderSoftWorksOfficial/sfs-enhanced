using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFS.Career;
using SFS.Input;
using SFS.Logs;
using SFS.Stats;
using SFS.Translations;
using SFS.UI;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class EndMissionMenu : Screen_Menu
{
	public static EndMissionMenu main;

	public GameObject menuHolder;

	public RectTransform logPrefab;

	public RectTransform logHolder;

	public TextAdapter logText;

	[Space]
	public RectTransform challengePrefab;

	public RectTransform challengeHolder;

	public GameObject noNewChallengesText;

	[Space]
	public TextAdapter titleText;

	public GameObject challengesMenu;

	public GameObject logMenu;

	public TextAdapter backButtonText;

	public TextAdapter nextButtonText;

	private List<RectTransform> logInstances = new List<RectTransform>();

	private List<RectTransform> challengeInstances = new List<RectTransform>();

	private Player recoveryTarget;

	private bool recovered;

	private string buttonText;

	private bool askRate_0;

	private bool askRate_1;

	private bool askRate_2;

	protected override CloseMode OnEscape => CloseMode.Current;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		challengePrefab.gameObject.SetActive(value: false);
		menuHolder.SetActive(value: false);
	}

	public void OpenEndMissionMenu_CurrentPlayer()
	{
		OpenEndMissionMenu(PlayerController.main.player);
	}

	public void OpenEndMissionMenu(Player target)
	{
		if (target == null)
		{
			return;
		}
		recoveryTarget = target;
		if (target is Rocket rocket)
		{
			bool recover = MapRocket.CanRecover(rocket, checkAchievements: false);
			StatsRecorder log = rocket.stats;
			Location location = rocket.location.Value;
			if (!rocket.partHolder.HasModule<CrewModule>() && !rocket.hasControl.Value)
			{
				Field text = (recover ? Loc.main.Debris_Recover : Loc.main.Debris_Destroy);
				MenuGenerator.ShowChoices(() => recover ? Loc.main.Debris_Recover_Title : Loc.main.Debris_Destroy_Title, ButtonBuilder.CreateButton(null, () => text, delegate
				{
					OpenMenu(null, openMenu: false, recovered: false, log, location);
				}, CloseMode.Current), ButtonBuilder.CreateButton(null, () => Loc.main.View_Mission_Log, delegate
				{
					OpenMenu(text, openMenu: true, recovered: false, log, location);
				}, CloseMode.Current), ButtonBuilder.CreateButton(null, () => Loc.main.Cancel, null, CloseMode.Current));
			}
			else if (rocket.partHolder.GetModules<CrewModule>().Any((CrewModule c) => c.HasCrew))
			{
				MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Crewed_Destroy_Warning, () => Loc.main.Destroy_Rocket, delegate
				{
					OpenMenu(Loc.main.Destroy_Rocket, openMenu: true, recovered: false, log, location);
				});
			}
			else
			{
				OpenMenu(recover ? Loc.main.Recover_Rocket : Loc.main.Destroy_Rocket, openMenu: true, recover, log, location);
			}
		}
		else if (target is Astronaut_EVA astronaut_EVA && MapAstronaut.CanRecover(astronaut_EVA))
		{
			OpenMenu(Loc.main.Recover_Rocket, openMenu: true, recovered: true, astronaut_EVA.stats, astronaut_EVA.location.Value);
		}
		void OpenMenu(string buttonText, bool openMenu, bool recovered, StatsRecorder statsRecorder, Location location2)
		{
			this.recovered = recovered;
			this.buttonText = buttonText;
			if (openMenu)
			{
				Open();
				DrawLog(statsRecorder.branch, location2);
				if (recovered)
				{
					DrawChallenges(statsRecorder);
				}
				SetTab(recovered);
			}
			else
			{
				Complete();
			}
		}
	}

	public void OnBackButton()
	{
		if (recovered && logMenu.activeSelf)
		{
			SetTab(challenges: true);
		}
		else
		{
			Close();
		}
	}

	public void OnNextButton()
	{
		if (challengesMenu.activeSelf)
		{
			SetTab(challenges: false);
		}
		else
		{
			Complete();
		}
	}

	private void SetTab(bool challenges)
	{
		titleText.Text = (challenges ? Loc.main.End_Challenges_Title : Loc.main.End_Logs_Title);
		challengesMenu.SetActive(challenges);
		logMenu.SetActive(!challenges);
		backButtonText.Text = ((challenges || !recovered) ? Loc.main.Cancel : Loc.main.Back_To_Challenges);
		nextButtonText.Text = (challenges ? ((string)Loc.main.Continue_To_Log) : buttonText);
	}

	private void DrawLog(int branch, Location location)
	{
		foreach (RectTransform logInstance in logInstances)
		{
			UnityEngine.Object.DestroyImmediate(logInstance.gameObject);
		}
		logInstances.Clear();
		List<(string, double, LogId)> list = ReplayMission(branch, location, out askRate_0, out askRate_1, out askRate_2);
		HashSet<LogId> previousInMission = new HashSet<LogId>();
		StringBuilder achievementsText_PC = new StringBuilder();
		double num = 0.0;
		foreach (var item2 in list)
		{
			bool flag = !Base.worldBase.IsCareer || IsNewLog(item2.Item3, ref previousInMission);
			if (!previousInMission.Contains(item2.Item3) && item2.Item3.type != SFS.Logs.LogType.Null)
			{
				previousInMission.Add(item2.Item3);
			}
			double item = item2.Item2;
			if (flag)
			{
				num += item;
			}
			string text = (flag ? "<color=white>" : "<color=#ffffff80>");
			string a = text + "- " + item2.Item1 + "</color>";
			string b = (Base.worldBase.IsCareer ? (text + (recovered ? (flag ? item : 0.0).ToFundsString() : "-") + "</color>") : "");
			Create(a, b);
		}
		if (recovered && Base.worldBase.IsCareer)
		{
			Create("", "\nTotal funds:\n" + num.ToFundsString());
		}
		logText.Text = achievementsText_PC.ToString();
		void Create(string value, string text2)
		{
			achievementsText_PC.AppendLine(value);
		}
	}

	private static bool IsNewLog(LogId a, ref HashSet<LogId> previousInMission)
	{
		if (!Base.worldBase.IsCareer)
		{
			return true;
		}
		if (IsNew(LogManager.main.completeLogs))
		{
			return IsNew(previousInMission);
		}
		return false;
		bool IsNew(HashSet<LogId> previous)
		{
			if (a.type == SFS.Logs.LogType.Reentry)
			{
				foreach (LogId previou in previous)
				{
					if (previou.type == SFS.Logs.LogType.Reentry && previou.planet == a.planet && previou.value > a.value)
					{
						return false;
					}
				}
			}
			return !previous.Contains(a);
		}
	}

	private void DrawChallenges(StatsRecorder log)
	{
		foreach (RectTransform challengeInstance in challengeInstances)
		{
			UnityEngine.Object.DestroyImmediate(challengeInstance.gameObject);
		}
		foreach (Challenge completeChallenge in log.challengeRecorder.GetCompleteChallenges())
		{
			if (!LogManager.main.completeChallenges.Contains(completeChallenge.id))
			{
				RectTransform item = Challenge.CreateChallengeUI(completeChallenge, complete: true, challengePrefab, challengeHolder, showDifficulty: false);
				challengeInstances.Add(item);
			}
		}
		noNewChallengesText.SetActive(challengeInstances.Count == 0);
	}

	public void Complete()
	{
		if (recoveryTarget == null)
		{
			return;
		}
		if ((bool)recoveryTarget.isPlayer)
		{
			if (recovered)
			{
				AskRate(out var asked);
				if (asked)
				{
					return;
				}
			}
			ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
			MenuGenerator.OpenMenu(CancelButton.None, CloseMode.None, new SizeSyncerBuilder(out var carrier), ButtonBuilder.CreateIconButton(carrier, buttonIcons.exit, () => Loc.main.Go_To_Space_Center, delegate
			{
				PickOption(GameManager.main.ExitToHub);
			}, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.newRocket, () => Loc.main.Build_New_Rocket, delegate
			{
				PickOption(GameManager.main.ExitToBuild);
			}, CloseMode.None), ButtonBuilder.CreateButton(carrier, () => Loc.main.Close, delegate
			{
				PickOption(null);
			}, CloseMode.Stack));
		}
		else
		{
			RemoveTarget();
			Close();
		}
		void PickOption(Action action)
		{
			RemoveTarget();
			action?.Invoke();
		}
		void RemoveTarget()
		{
			if (!(recoveryTarget == null))
			{
				if (recovered)
				{
					if (recoveryTarget is Rocket rocket)
					{
						CrewModule[] modules = rocket.partHolder.GetModules<CrewModule>();
						for (int i = 0; i < modules.Length; i++)
						{
							CrewModule.Seat[] seats = modules[i].seats;
							for (int j = 0; j < seats.Length; j++)
							{
								seats[j].Exit();
							}
						}
						SaveLogsAndRewardFunds(rocket.stats, rocket.location.Value);
					}
					else if (recoveryTarget is Astronaut_EVA astronaut_EVA)
					{
						SaveLogsAndRewardFunds(astronaut_EVA.stats, astronaut_EVA.location.Value);
					}
				}
				if (recoveryTarget is Rocket rocket2)
				{
					RocketManager.DestroyRocket(rocket2, DestructionReason.Intentional);
				}
				else if (recoveryTarget is Astronaut_EVA astronaut)
				{
					AstronautManager.DestroyEVA(astronaut, death: false);
				}
			}
		}
	}

	private void SaveLogsAndRewardFunds(StatsRecorder logRecorder, Location location)
	{
		List<(string, double, LogId)> list = ReplayMission(logRecorder.branch, location, out var _, out var _, out var _);
		double num = 0.0;
		HashSet<LogId> previousInMission = new HashSet<LogId>();
		foreach (var item2 in list)
		{
			if (IsNewLog(item2.Item3, ref previousInMission))
			{
				if (!previousInMission.Contains(item2.Item3) && item2.Item3.type != SFS.Logs.LogType.Null)
				{
					previousInMission.Add(item2.Item3);
				}
				num += item2.Item2;
			}
		}
		CareerState.main.RewardFunds(num);
		HashSet<LogId> completeLogs = LogManager.main.completeLogs;
		LogId[] array = completeLogs.ToArray();
		foreach (var item3 in list)
		{
			LogId item = item3.Item3;
			bool flag = false;
			if (item.type == SFS.Logs.LogType.Reentry)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].type == SFS.Logs.LogType.Reentry && array[i].planet == item.planet && item.value > array[i].value)
					{
						completeLogs.Remove(array[i]);
						completeLogs.Add(item);
						array[i] = item;
						flag = true;
						break;
					}
				}
			}
			if (!flag && !completeLogs.Contains(item) && item.type != SFS.Logs.LogType.Null)
			{
				completeLogs.Add(item);
			}
		}
		foreach (Challenge completeChallenge in logRecorder.challengeRecorder.GetCompleteChallenges())
		{
			if (!LogManager.main.completeChallenges.Contains(completeChallenge.id))
			{
				LogManager.main.completeChallenges.Add(completeChallenge.id);
			}
		}
	}

	private void AskRate(out bool asked)
	{
		asked = false;
	}

	public override void OnOpen()
	{
		menuHolder.SetActive(value: true);
	}

	public override void OnClose()
	{
		menuHolder.SetActive(value: false);
	}

	private static List<(string, double, LogId)> ReplayMission(int startBranch, Location location, out bool space, out bool orbit, out bool moonLand)
	{
		bool _space = false;
		bool _orbit = false;
		bool _moonLand = false;
		HashSet<int> isSplit = IsSplit(startBranch);
		HashSet<int> traversed = new HashSet<int>();
		Dictionary<int, State> copyForSplit = new Dictionary<int, State>();
		State state = Recuse(startBranch);
		LogsModule.Log_End(state.Log, location, state.landed);
		space = _space;
		orbit = _orbit;
		moonLand = _moonLand;
		return state.logs;
		State Recuse(int id)
		{
			Branch branch = LogManager.main.branches[id];
			traversed.Add(id);
			if (traversed.Contains(branch.parentA) || traversed.Contains(branch.parentB))
			{
				State obj = (copyForSplit.ContainsKey(branch.parentA) ? copyForSplit[branch.parentA] : copyForSplit[branch.parentB]);
				obj.ReplayLog(id, ref _space, ref _orbit, ref _moonLand);
				return obj;
			}
			State state2 = ((branch.parentA != -1) ? Recuse(branch.parentA) : null);
			State state3 = ((branch.parentB != -1) ? Recuse(branch.parentB) : null);
			if (state2 != null && state3 != null)
			{
				State state4 = State.Merge(state2, state3);
				if (!WasAstronaut())
				{
					state4.Log_Dock();
				}
				state4.ReplayLog(id, ref _space, ref _orbit, ref _moonLand);
				return state4;
			}
			if (isSplit.Contains(branch.parentA) || isSplit.Contains(branch.parentB))
			{
				int key = (isSplit.Contains(branch.parentA) ? branch.parentA : branch.parentB);
				copyForSplit.Add(key, (state2 ?? state3).CopyState(branch.startTime));
				State obj2 = state2 ?? state3;
				obj2.ReplayLog(id, ref _space, ref _orbit, ref _moonLand);
				return obj2;
			}
			State state5 = state2 ?? state3;
			if (state5 == null)
			{
				state5 = new State(new List<(string, double, LogId)>(), branch.startTime);
			}
			state5.ReplayLog(id, ref _space, ref _orbit, ref _moonLand);
			return state5;
			bool WasAstronaut()
			{
				List<List<string>> logEvents = LogManager.main.branches[branch.parentB].logEvents;
				if (logEvents.Count > 0)
				{
					return logEvents[0][0] == StatsRecorder.EventType.LeftCapsule.ToString();
				}
				return false;
			}
		}
	}

	private static HashSet<int> IsSplit(int startBranch)
	{
		HashSet<int> output = new HashSet<int>();
		HashSet<int> traversed = new HashSet<int>();
		CollectParentBranches(startBranch);
		return output;
		void CollectParentBranches(int branch)
		{
			if (LogManager.main.branches.ContainsKey(branch))
			{
				if (traversed.Contains(branch))
				{
					output.Add(branch);
				}
				else
				{
					traversed.Add(branch);
					Branch branch2 = LogManager.main.branches[branch];
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
	}
}
