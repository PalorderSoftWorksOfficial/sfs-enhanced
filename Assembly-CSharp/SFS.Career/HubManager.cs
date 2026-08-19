using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Cameras;
using SFS.Core;
using SFS.Input;
using SFS.Logs;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;
using UnityEngine.Analytics;

namespace SFS.Career;

public class HubManager : MonoBehaviour
{
	public static HubManager main;

	public TextAdapter[] fundsTexts;

	public GameObject funds;

	[Space]
	public TextAdapter researchButtonText;

	public BasicMenu researchTab;

	public TreeComponent tree_Research;

	[Space]
	public SpaceCenter spaceCenter;

	public CameraManager worldCamera;

	public WorldEnvironment environment;

	public PostProcessing postProcessing;

	[Space]
	public Screen_Game hub_Input;

	[Space]
	public GameObject challengesInfo;

	public TextAdapter challengesInfoTitle;

	public TextAdapter challengesInfoText;

	public TextAdapter challengesButtonText;

	public BasicMenu challengesTab;

	public ScrollElement challengesScroller;

	public RectTransform planetTitlePrefab;

	[Space]
	public Button resumeGameButton;

	public GameObject challengesButton;

	public GameObject astronautsButton;

	public GameObject tutorialsButton;

	[Space]
	private WorldSave state;

	public HubManager()
	{
		main = this;
	}

	private void Start()
	{
		planetTitlePrefab.gameObject.SetActive(value: false);
		ActiveCamera.Camera = worldCamera;
		LoadPersistent();
		SavingCache.main.Preload_BlueprintPersistent();
		SavingCache.main.Preload_WorldPersistent(needsRocketsAndBranches: true);
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_10000Ms = (Action)Delegate.Combine(timeEvent.on_10000Ms, new Action(UpdatePersistent));
		funds.SetActive(Base.worldBase.IsCareer);
		challengesInfo.SetActive(!Base.worldBase.IsCareer);
		resumeGameButton.SetEnabled(Base.worldBase.paths.CanResumeGame());
		tutorialsButton.SetActive(value: false);
		DrawChallenges();
		Location value = spaceCenter.vab.building.location.Value;
		value.position.y += 75.0;
		WorldView.main.SetViewLocation(value);
		environment.Initialize(launchPlanetOnly: true);
		hub_Input.keysNode.AddOnKeyDown(KeybindingsPC.keys.Close_Menu, OpenMenu);
		if (RemoteSettings.GetBool("Hard_Game_Msg", defaultValue: true) && AnalyticsSessionInfo.sessionCount == 1 && FileLocations.GetOneTimeNotification("Hard_Game_Msg"))
		{
			MenuGenerator.ShowChoices(() => "Don't give up, keep trying and you will succeed!\n\n\n", ButtonBuilder.CreateButton(null, () => "Continue", delegate
			{
			}, CloseMode.Current));
			MenuGenerator.ShowChoices(() => "Rocket science is hard\n\n\n", ButtonBuilder.CreateButton(null, () => "Continue", delegate
			{
			}, CloseMode.Current));
			MenuGenerator.ShowChoices(() => "This is a challenging game that\nrequires patience and perseverance\n\n", ButtonBuilder.CreateButton(null, () => "Continue", delegate
			{
			}, CloseMode.Current));
		}
	}

	private void OnDestroy()
	{
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_10000Ms = (Action)Delegate.Remove(timeEvent.on_10000Ms, new Action(UpdatePersistent));
	}

	private void DrawChallenges()
	{
		List<(Planet planet, List<Challenge> missions)> planets = new List<(Planet, List<Challenge>)>();
		Add(Base.planetLoader.planets.Values.FirstOrDefault((Planet a) => a.parentBody == null));
		Challenge[] challengesArray = Base.worldBase.challengesArray;
		foreach (Challenge mission in challengesArray)
		{
			planets.FirstOrDefault(((Planet planet, List<Challenge> missions) p) => p.planet == mission.owner).missions.Add(mission);
		}
		RectTransform component = planetTitlePrefab.GetChild(1).GetChild(0).GetComponent<RectTransform>();
		component.gameObject.SetActive(value: false);
		int num2 = 0;
		int num3 = 0;
		foreach (var (planet, list) in planets)
		{
			if (list.Count == 0)
			{
				continue;
			}
			RectTransform rectTransform = UnityEngine.Object.Instantiate(planetTitlePrefab, challengesScroller.transform);
			rectTransform.gameObject.SetActive(value: true);
			challengesScroller.RegisterScrolling(rectTransform.GetChild(0).GetComponent<Button>());
			Transform child = rectTransform.GetChild(1);
			Transform child2 = child.GetChild(1);
			int num4 = 0;
			foreach (Challenge item in list)
			{
				bool flag = state.completeChallenges.Contains(item.id);
				Challenge.CreateChallengeUI(item, flag, component, child, showDifficulty: true);
				if (flag)
				{
					num4++;
				}
			}
			child2.SetAsLastSibling();
			bool flag2 = num4 < 5 && planet.codeName == "Earth";
			MoveModule component2 = rectTransform.GetComponent<MoveModule>();
			Float_Reference targetTime = component2.targetTime;
			float value = (component2.time.Value = (flag2 ? 1 : 0));
			targetTime.Value = value;
			rectTransform.GetComponentInChildren<TextAdapter>().Text = string.Concat(planet.DisplayName, " ", num4.ToString(), "/", list.Count.ToString());
			num2 += num4;
			num3 += list.Count;
		}
		challengesInfoTitle.Text = "Challenges";
		challengesInfoText.Text = num2 + "/" + num3;
		challengesButtonText.Text = Loc.main.Challenges_Button.Inject(num2.ToString(), "complete").Inject(num3.ToString(), "total");
		void Add(Planet a)
		{
			planets.Add((a, new List<Challenge>()));
			Planet[] satellites = a.satellites;
			for (int i = 0; i < satellites.Length; i++)
			{
				Add(satellites[i]);
			}
		}
	}

	public void OpenMenu()
	{
		ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
		MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize), ButtonBuilder.CreateIconButton(carrier, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None), ElementGenerator.VerticalSpace(10), ButtonBuilder.CreateIconButton(carrier, buttonIcons.resume, () => Loc.main.Resume_Game, ResumeGame, CloseMode.None).CustomizeButton(delegate(Button b)
		{
			b.SetEnabled(Base.worldBase.paths.CanResumeGame());
		}), ButtonBuilder.CreateIconButton(carrier, buttonIcons.exit, () => Loc.main.Exit_To_Main_Menu, ExitToMainMenu, CloseMode.None));
	}

	public void ExitToMainMenu()
	{
		UpdatePersistent();
		Base.sceneLoader.LoadHomeScene(null);
	}

	public void EnterBuild()
	{
		if (Base.worldBase.IsCareer && CareerState.main.state.funds > 0.0 && CareerState.main.state.unlocked_Upgrades.Count == 0)
		{
			MsgDrawer.main.Log("New part unlock is available");
			return;
		}
		UpdatePersistent();
		Base.sceneLoader.LoadBuildScene(askBuildNew: true);
	}

	public void ResumeGame()
	{
		UpdatePersistent();
		Base.sceneLoader.LoadWorldScene();
	}

	public void OpenResearch()
	{
		researchTab.Open();
	}

	public void OpenAchievements()
	{
		challengesTab.Open();
	}

	public void OpenAstronauts()
	{
		AstronautMenu.main.OpenMenu(null, null);
	}

	public void OpenTutorials()
	{
		HomeManager.OpenTutorials_Static();
	}

	public void CloseResearch()
	{
		if (Base.worldBase.IsCareer && CareerState.main.state.funds > 0.0 && CareerState.main.state.unlocked_Upgrades.Count == 0)
		{
			MsgDrawer.main.Log("New part unlock is available");
		}
		else
		{
			researchTab.Close();
		}
	}

	public void UpdateCareerProgressText()
	{
		var (num, num2) = TT_Creator.GetProgress(tree_Research);
		researchButtonText.Text = Loc.main.Research_And_Development.Inject((num - 4).ToString(), "complete").Inject((num2 - 4).ToString(), "total");
	}

	private void LoadPersistent()
	{
		MsgCollector logger = new MsgCollector();
		state = SavingCache.main.LoadWorldPersistent(logger, needsRocketsAndBranches: false, eraseCache: false);
		CareerState.main.SetState(state.career);
		AstronautState.main.state = state.astronauts;
		if (logger.msg.Length > 0)
		{
			ActionQueue.main.QueueMenu(Menu.read.Create(() => logger.msg.ToString()));
		}
	}

	private void UpdatePersistent()
	{
		state.version = Application.version;
		SavingCache.main.SaveWorldPersistent(state, cache: true, saveRocketsAndBranches: false, addToRevert: false, deleteRevert: false);
	}
}
