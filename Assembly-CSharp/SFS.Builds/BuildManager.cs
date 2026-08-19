using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Cameras;
using SFS.Career;
using SFS.Input;
using SFS.Parsers.Json;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.World;
using SFS.World.PlanetModules;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

namespace SFS.Builds;

public class BuildManager : MonoBehaviour
{
	[Serializable]
	public class ExampleRocket
	{
		public TranslationVariable rocketName;

		public string json;
	}

	public static BuildManager main;

	public Screen_Game build_Input;

	[Space]
	public BuildCamera buildCamera;

	public BuildMenus buildMenus;

	public PickGridUI pickGrid;

	public HoldGrid holdGrid;

	public BuildGrid buildGrid;

	public GridSize buildGridSize;

	public BuildSelector selector;

	public AreaSelect areaSelect;

	[Space]
	public DownloadMenu downloadMenu;

	[Space]
	public SoundEffect destroyPartSound;

	public MusicPlaylistPlayer music;

	[Space]
	public PostProcessing postProcessing;

	[Space]
	public SFS.UI.Button launchButton;

	public PopupAnimation noFuelSourcePopup;

	private Vector2? noFuelSourcePopupData;

	public RectTransform fullVersionButton;

	public bool symmetryMode;

	private OpenTracker partMenuReference;

	public ExampleRocket[] exampleRockets;

	public BuildManager()
	{
		main = this;
	}

	private void Start()
	{
		Menu.load.implementation = new Blueprint_Saving();
		build_Input.onOpen.AddListener(delegate
		{
			buildMenus.showBuildMenus.Value = true;
		});
		build_Input.onClose.AddListener(delegate
		{
			buildMenus.showBuildMenus.Value = false;
		});
		ScreenManager.main.selfInitialize = false;
		ScreenManager.main.SetStack(() => build_Input);
		buildGridSize.Initialize();
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_10000Ms = (Action)Delegate.Combine(timeEvent.on_10000Ms, new Action(BuildState.main.UpdatePersistent_NoCache));
		postProcessing.SetAmbient(new PostProcessingModule.Key(0f, 1.4f, 0f, 0f, 1f, 1.15f, 1f, 1f, 1f));
		Screen_Game screen_Game = build_Input;
		screen_Game.onInputStart = (Action<OnInputStartData>)Delegate.Combine(screen_Game.onInputStart, new Action<OnInputStartData>(OnInputStart));
		Screen_Game screen_Game2 = build_Input;
		screen_Game2.onNotStationary = (Action<OnNotStationary>)Delegate.Combine(screen_Game2.onNotStationary, new Action<OnNotStationary>(OnInputMove));
		Screen_Game screen_Game3 = build_Input;
		screen_Game3.onInputEnd = (Action<OnInputEndData>)Delegate.Combine(screen_Game3.onInputEnd, new Action<OnInputEndData>(OnInputEnd));
		Screen_Game screen_Game4 = build_Input;
		screen_Game4.onTouchLongClick = (Action<OnTouchLongClickData>)Delegate.Combine(screen_Game4.onTouchLongClick, new Action<OnTouchLongClickData>(OnTouchLongClick));
		Screen_Game screen_Game5 = build_Input;
		screen_Game5.onZoom = (Action<ZoomData>)Delegate.Combine(screen_Game5.onZoom, new Action<ZoomData>(OnZoom));
		Screen_Game screen_Game6 = build_Input;
		screen_Game6.onDrag = (Action<DragData>)Delegate.Combine(screen_Game6.onDrag, new Action<DragData>(OnDrag));
		BuildState.main.LoadPersistent();
		if (Base.sceneLoader.sceneSettings.askBuildNew && buildGrid.activeGrid.partsHolder.parts.Count > 0)
		{
			ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
			MenuGenerator.OpenMenu(CancelButton.None, CloseMode.Stack, new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize), ButtonBuilder.CreateIconButton(carrier, buttonIcons.newRocket, () => Loc.main.Build_New_Rocket, delegate
			{
				OnPick(delegate
				{
					BuildState.main.Clear(applyUndo: false);
				});
			}, CloseMode.Stack), ButtonBuilder.CreateIconButton(carrier, buttonIcons.resume, () => Loc.main.Expand_Last_Rocket, delegate
			{
				OnPick(null);
			}, CloseMode.Stack));
		}
		else
		{
			OnPick(null);
		}
		music.StartPlaying(3f);
		static void OnPick(Action pick)
		{
			pick?.Invoke();
		}
	}

	private void OnDestroy()
	{
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_10000Ms = (Action)Delegate.Remove(timeEvent.on_10000Ms, new Action(BuildState.main.UpdatePersistent_NoCache));
	}

	private void OnInputStart(OnInputStartData data)
	{
		if (data.inputType == InputType.Touch || data.inputType == InputType.MouseLeft)
		{
			On_LeftOrTouch_Start(data);
		}
		if (data.inputType == InputType.MouseRight)
		{
			areaSelect.StartSelect(data.position.World(0f));
		}
	}

	private void OnInputMove(OnNotStationary data)
	{
		if (data.inputType == InputType.Touch || data.inputType == InputType.MouseLeft)
		{
			On_LeftOrTouch_Move(data);
		}
	}

	private void OnInputEnd(OnInputEndData data)
	{
		if (data.inputType == InputType.Touch || data.inputType == InputType.MouseLeft)
		{
			On_LeftOrTouch_End(data);
		}
		if (data.click && data.inputType == InputType.MouseRight)
		{
			On_RightOrLong_Click(data.position, longClick: false);
		}
		if (data.inputType == InputType.MouseRight)
		{
			areaSelect.EndSelect();
		}
	}

	private void OnTouchLongClick(OnTouchLongClickData data)
	{
		On_RightOrLong_Click(data.position, longClick: true);
	}

	private void On_LeftOrTouch_Start(OnInputStartData data)
	{
		buildCamera.dragging = true;
		if (InputManager.GetTouchesCountOnElement(build_Input) == 1)
		{
			holdGrid.OnInputStart(data);
			return;
		}
		holdGrid.OnAnotherInputStart();
		areaSelect.CancelSelect();
	}

	private void On_LeftOrTouch_Move(OnNotStationary data)
	{
		if (holdGrid.ConfirmTakeParts_BuildGrid())
		{
			partMenuReference?.Close();
		}
	}

	private void On_LeftOrTouch_End(OnInputEndData data)
	{
		buildCamera.dragging = false;
		if (holdGrid.HasParts(includePreHeld: true))
		{
			bool flag = holdGrid.HasParts(includePreHeld: false);
			holdGrid.OnInputEnd(data);
			if (data.click && !flag && buildGrid.RaycastParts(data.position.World(0f), out var hit))
			{
				buildMenus.OnPartClick(hit);
			}
		}
		else if (areaSelect.Selecting)
		{
			areaSelect.EndSelect();
		}
		else if (data.click)
		{
			buildMenus.OnEmptyClick();
		}
	}

	private void On_RightOrLong_Click(TouchPosition position, bool longClick)
	{
		if (Part_Utility.RaycastParts(holdGrid.holdGrid.partsHolder.parts.ToArray(), position.World(0f), 0.3f, out var hit) || buildGrid.RaycastParts(position.World(0f), out hit))
		{
			partMenuReference = buildMenus.partMenu.Open_DrawPart(null, new Part[1] { hit.part }, PartDrawSettings.BuildSettings, AttachWithArrow.FollowPart(hit.part), dontUpdateOnZoomChange: false, skipAnimation: false);
		}
		else if (longClick)
		{
			areaSelect.StartSelect(position.World(0f));
		}
	}

	private void OnDrag(DragData data)
	{
		if (holdGrid.draggingParts)
		{
			holdGrid.position.Value -= data.DeltaWorld(0f);
		}
		else if (areaSelect.Selecting)
		{
			areaSelect.Drag(-data.DeltaWorld(0f));
		}
		else
		{
			buildCamera.CameraPosition += data.DeltaWorld(0f);
		}
	}

	private void OnZoom(ZoomData data)
	{
		Vector2 vector = data.zoomPosition.World(0f);
		buildCamera.CameraDistance *= data.zoomDelta;
		buildCamera.CameraPosition += vector - data.zoomPosition.World(0f);
	}

	public void Launch()
	{
		PartHolder partsHolder = buildGrid.activeGrid.partsHolder;
		if (!DevSettings.DisableAstronauts)
		{
			CrewModule[] modules = partsHolder.GetModules<CrewModule>();
			for (int i = 0; i < modules.Length; i++)
			{
				modules[i].needsCrewForControl.Value = true;
			}
		}
		string textWarnings = "";
		if (!partsHolder.GetModules<ControlModule>().Any((ControlModule c) => c.hasControl.Value))
		{
			AddWarning((!DevSettings.DisableAstronauts && partsHolder.HasModule<CrewModule>()) ? Loc.main.Mission_Crew : Loc.main.Missing_Capsule);
		}
		if (!partsHolder.HasModule<ParachuteModule>())
		{
			AddWarning(Loc.main.Missing_Parachute);
		}
		if (!partsHolder.HasModule<HeatInfoModule>() && CareerState.main.HasFeature(WorldSave.CareerState.heatShieldFeature) && AnalyticsSessionInfo.sessionCount > 1)
		{
			AddWarning(Loc.main.Missing_Heat_Shield);
		}
		if (buildMenus.statsDrawer.TWR < 1f)
		{
			AddWarning(Loc.main.Too_Heavy.Inject(buildMenus.statsDrawer.mass.ToMassString(forceDecimal: false), "mass").Inject(buildMenus.statsDrawer.thrust.ToThrustString(), "thrust").GetText()
				.Replace("\n\n\n", "\n"));
		}
		if (textWarnings != "")
		{
			MenuGenerator.OpenConfirmation(CloseMode.Current, () => textWarnings, () => Loc.main.Launch_Anyway_Button, Launch_);
		}
		else
		{
			Launch_();
		}
		void AddWarning(string warning)
		{
			textWarnings = textWarnings + ((textWarnings != "") ? "\n\n" : string.Concat(Loc.main.Warnings_Title, "\n\n")) + "- " + warning;
		}
		static void Launch_()
		{
			if (BuildOrientation.main.rotation != 0f)
			{
				MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Launch_Horizontally_Ask, () => Loc.main.Launch_Horizontally_Confirm, delegate
				{
					Launch_2(forceVertical: false);
				}, () => Loc.main.Launch_Vertically_Confirm, delegate
				{
					Launch_2(forceVertical: true);
				});
			}
			else
			{
				Launch_2(forceVertical: false);
			}
		}
		static void Launch_2(bool forceVertical)
		{
			BuildState.main.UpdatePersistent(forceVertical);
			Base.sceneLoader.LoadWorldScene(launch: true);
		}
	}

	private void LateUpdate()
	{
		if (noFuelSourcePopupData.HasValue)
		{
			noFuelSourcePopup.transform.position = ActiveCamera.main.activeCamera.Value.camera.WorldToScreenPoint(noFuelSourcePopupData.Value) + Vector3.up * 35f;
		}
	}

	private void ResetNoFuelSourcePopup()
	{
		noFuelSourcePopup.Disable();
		noFuelSourcePopupData = null;
		launchButton.GetComponentInChildren<Text>().text = Loc.main.Launch_Button;
	}

	public void OpenMenu()
	{
		List<MenuElement> list = new List<MenuElement>();
		list.Add(new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize));
		list.Add(new SizeSyncerBuilder(out var carrier2).HorizontalMode(SizeMode.MaxChildSize));
		ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.save, () => Loc.main.Save_Blueprint, OpenSaveMenu, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.load, () => Loc.main.Load_Blueprint, OpenLoadMenu, CloseMode.None)));
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.moveRocket, () => Loc.main.Move_Rocket_Button, MoveRocket, CloseMode.Current), ButtonBuilder.CreateIconButton(carrier, buttonIcons.clear, () => Loc.main.Clear_Confirm, AskClear, CloseMode.Current)));
		if (RemoteSettings.GetBool("Example_Rockets", defaultValue: true) || RemoteSettings.GetBool("Video_Tutorials", defaultValue: true))
		{
			list.Add(ElementGenerator.VerticalSpace(25));
		}
		if (RemoteSettings.GetBool("Example_Rockets", defaultValue: true))
		{
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exampleRockets, () => Loc.main.Example_Rockets_OpenMenu, OpenExampleRocketsMenu, CloseMode.None));
		}
		if (RemoteSettings.GetBool("Video_Tutorials", defaultValue: true))
		{
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.videoTutorials, () => Loc.main.Video_Tutorials_OpenButton, HomeManager.OpenTutorials_Static, CloseMode.None));
		}
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.shareRocket, () => Loc.main.Share_Button, UploadPC, CloseMode.Current));
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None));
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exit_Resume, () => Loc.main.Resume_Game, ResumeGame, CloseMode.None).CustomizeButton(delegate(SFS.UI.Button b)
		{
			b.SetEnabled(Base.worldBase.paths.CanResumeGame());
		}));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exit, () => Loc.main.Exit_To_Space_Center, ExitToHub, CloseMode.None));
		MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, list.ToArray());
		static void ExitToHub()
		{
			BuildState.main.UpdatePersistent();
			Base.sceneLoader.LoadHubScene();
		}
		void MoveRocket()
		{
			selector.Select(buildGrid.activeGrid.partsHolder.GetArray());
		}
	}

	public void ResumeGame()
	{
		BuildState.main.UpdatePersistent();
		Base.sceneLoader.LoadWorldScene();
	}

	public void OpenLoadMenu()
	{
		Menu.load.Open();
	}

	public void OpenSaveMenu()
	{
		Menu.load.OpenSaveMenu();
	}

	public void AskClear()
	{
		MenuGenerator.OpenConfirmation(CloseMode.Stack, () => Loc.main.Clear_Warning, () => Loc.main.Clear_Confirm, delegate
		{
			Undo.main.CreateNewStep("Clear");
			BuildState.main.Clear(applyUndo: true);
		});
	}

	public void UploadPC()
	{
		downloadMenu.OpenSharingMenu_PC();
	}

	public void ExitToHome()
	{
		BuildState.main.UpdatePersistent();
		Base.sceneLoader.LoadHomeScene(null);
	}

	public void ToggleSymmetryMode()
	{
		symmetryMode = !symmetryMode;
		buildMenus.symmetryButtonPc.SetSelected(symmetryMode);
		MsgDrawer.main.Log(symmetryMode ? Loc.main.Symmetry_On : Loc.main.Symmetry_Off);
		PopupManager.MarkUsed(PopupManager.Feature.Symmetry);
	}

	public void ToggleStagingMode()
	{
		Undo.main.RecordOtherStep(buildMenus.stagingMode.Value ? Undo.OtherAction.Type.CloseStaging : Undo.OtherAction.Type.OpenStaging);
		buildMenus.stagingMode.Value = !buildMenus.stagingMode.Value;
	}

	public void OpenPartsSalePage()
	{
		Base.sceneLoader.LoadHomeScene("Parts");
	}

	public void OpenSkinsSalePage()
	{
		Base.sceneLoader.LoadHomeScene("Skins");
	}

	public void OpenFullVersionSalePage()
	{
		Base.sceneLoader.LoadHomeScene("Full Version");
	}

	private void OpenExampleRocketsMenu()
	{
		List<MenuElement> list = new List<MenuElement> { new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize) };
		ExampleRocket[] array = exampleRockets;
		foreach (ExampleRocket exampleRocket in array)
		{
			list.Add(ButtonBuilder.CreateButton(carrier, () => exampleRocket.rocketName.Field, delegate
			{
				Undo.main.CreateNewStep("Load example rocket");
				BuildState.main.LoadBlueprint(JsonWrapper.FromJson<Blueprint>(exampleRocket.json), Menu.read, autoCenterParts: false, applyUndo: true);
			}, CloseMode.Stack));
		}
		MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Stack, list.ToArray());
	}
}
