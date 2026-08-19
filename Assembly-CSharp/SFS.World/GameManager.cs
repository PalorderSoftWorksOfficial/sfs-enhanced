using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SFS.Career;
using SFS.Core;
using SFS.Input;
using SFS.Logs;
using SFS.Stats;
using SFS.Translations;
using SFS.Tutorials;
using SFS.UI;
using SFS.Utilities;
using SFS.Variables;
using SFS.World.Maps;
using SFS.World.Terrain;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.World;

public class GameManager : MonoBehaviour
{
	public static GameManager main;

	public WorldEnvironment environment;

	public Screen_Game world_Input;

	public Screen_Game map_Input;

	public AeroData aeroData;

	public List<Rocket> rockets = new List<Rocket>();

	public GameObject fuelManager;

	private float timeSinceLastSave;

	public GameManager()
	{
		main = this;
	}

	private void Start()
	{
		StagingDrawer.main.Initialize_Game();
		AddOnKeyDown(KeybindingsPC.keys.Close_Menu, OpenMenu);
		AddOnKeyDown(KeybindingsPC.keys.SaveLoad[0], Menu.load.OpenSaveMenu);
		AddOnKeyDown(KeybindingsPC.keys.SaveLoad[1], Menu.load.Open);
		TerrainColliderManager.main.hasColliders.OnChange += new Action(UpdateCanVelocityOffset);
		PlayerController.main.player.OnChange += new Action(UpdateCanVelocityOffset);
		Menu.load.implementation = new Quicksave_Saving(Base.worldBase.paths, CreateWorldSave, delegate(WorldSave save, I_MsgLogger logger)
		{
			new Thread(Revert.DeleteAll).Start();
			LoadSave(save, forLaunch: false, logger);
		});
		ScreenManager.main.SetStack(() => (!Map.manager.mapMode.Value) ? world_Input : map_Input);
		Map.manager.mapMode.OnChange += (Action)delegate
		{
			if (ScreenManager.main.CurrentScreen == map_Input || ScreenManager.main.CurrentScreen == world_Input)
			{
				ScreenManager.main.SetStack(() => (!Map.manager.mapMode.Value) ? world_Input : map_Input);
			}
		};
		Map.environment.CreateReferences();
		try
		{
			new Thread(Revert.DeleteAll).Start();
			LoadPersistentAndLaunch();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		environment.Initialize(launchPlanetOnly: false);
		Map.environment.CreateEnvironments();
		static void UpdateCanVelocityOffset()
		{
			WorldView.main.canVelocityOffset.Value = !TerrainColliderManager.main.hasColliders.Value && PlayerController.main.player.Value != null;
			if (PlayerController.main.player.Value != null)
			{
				PlayerController.main.TrackPlayer();
			}
		}
	}

	public static void AddAxis((I_Key, I_Key) axis, ref Float_Local output)
	{
		main.world_Input.keysNode.AddAxis(axis, ref output);
		main.map_Input.keysNode.AddAxis(axis, ref output);
	}

	public static void AddOnKeyDown(I_Key key, Action action)
	{
		main.world_Input.keysNode.AddOnKeyDown(key, action);
		main.map_Input.keysNode.AddOnKeyDown(key, action);
	}

	public static void AddOnKey(I_Key key, Action action)
	{
		main.world_Input.keysNode.AddOnKey(key, action);
		main.map_Input.keysNode.AddOnKey(key, action);
	}

	public void OpenMenu()
	{
		List<MenuElement> list = new List<MenuElement>();
		list.Add(new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize));
		list.Add(new SizeSyncerBuilder(out var carrier2).HorizontalMode(SizeMode.MaxChildSize));
		ResourcesLoader.ButtonIcons buttonIcons = ResourcesLoader.main.buttonIcons;
		if (Base.worldBase.settings.mode.allowQuicksaves)
		{
			list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.save, () => Loc.main.Create_Quicksave, OpenSave, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.load, () => Loc.main.Load_Quicksave, OpenLoad, CloseMode.None)));
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.revert, () => Loc.main.Restart_Mission_To_Launch, delegate
			{
				RevertToLaunch(skipConfirmation: false);
			}, CloseMode.None), ButtonBuilder.CreateIconButton(carrier, buttonIcons.revert, () => Loc.main.Restart_Mission_To_Build, delegate
			{
				RevertToBuild(skipConfirmation: false);
			}, CloseMode.None)));
			list.Add(ElementGenerator.DefaultHorizontalGroup(ButtonBuilder.CreateIconButton(carrier, buttonIcons.revert, () => Loc.main.Revert_30_Secs, FailureMenu.main.Revert_30_Sec, CloseMode.Current).CustomizeButton(delegate(Button b)
			{
				b.SetEnabled(Revert.HasRevert_30_Sec());
			}), ButtonBuilder.CreateIconButton(carrier, buttonIcons.revert, () => Loc.main.Revert_3_Min, FailureMenu.main.Revert_3_Min, CloseMode.Current).CustomizeButton(delegate(Button b)
			{
				b.SetEnabled(Revert.HasRevert_3_Min());
			})));
		}
		list.Add(ElementGenerator.VerticalSpace(25));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.newRocket, () => Loc.main.Build_New_Rocket, ExitToBuild, CloseMode.None));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.clear, () => Loc.main.Clear_Debris_Confirm, OpenClearDebrisMenu, CloseMode.None));
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.cheats, () => Loc.main.Open_Cheats_Button, SandboxSettings.main.Open, CloseMode.None));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.settings, () => Loc.main.Open_Settings_Button, Menu.settings.Open, CloseMode.None));
		if (RemoteSettings.GetBool("Video_Tutorials", defaultValue: true))
		{
			list.Add(ElementGenerator.VerticalSpace(10));
			list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.videoTutorials, () => Loc.main.Video_Tutorials_OpenButton, HomeManager.OpenTutorials_Static, CloseMode.None));
		}
		list.Add(ElementGenerator.VerticalSpace(10));
		list.Add(ButtonBuilder.CreateIconButton(carrier2, buttonIcons.exit, () => Loc.main.Exit_To_Space_Center, ExitToHub, CloseMode.None));
		MenuGenerator.OpenMenu(CancelButton.Close, CloseMode.Current, list.ToArray());
	}

	public void OpenSave()
	{
		Menu.load.OpenSaveMenu();
	}

	public void OpenLoad()
	{
		Menu.load.Open();
	}

	public void RevertToLaunch(bool skipConfirmation)
	{
		if (!SavingCache.main.TryLoadRevertToLaunch(out var revertSave))
		{
			return;
		}
		if ((WorldTime.main.worldTime - revertSave.state.worldTime > 15.0 || FileLocations.GetOneTimeNotification("Revert_Functionality")) && !skipConfirmation)
		{
			MenuGenerator.OpenConfirmation(CloseMode.Stack, () => Loc.main.Restart_Mission_To_Launch_Warning, () => Loc.main.Restart_Mission_To_Launch, Confirm);
		}
		else
		{
			ScreenManager.main.CloseStack();
			Confirm();
		}
		void Confirm()
		{
			Revert.DeleteAll();
			LoadAndLaunch(revertSave);
		}
	}

	public void RevertToBuild(bool skipConfirmation)
	{
		if (!SavingCache.main.TryLoadRevertToLaunch(out var revertSave))
		{
			return;
		}
		if (WorldTime.main.worldTime - revertSave.state.worldTime > 15.0 && !skipConfirmation)
		{
			MenuGenerator.OpenConfirmation(CloseMode.Stack, () => Loc.main.Restart_Mission_To_Build_Warning, () => Loc.main.Restart_Mission_To_Build, Confirm);
		}
		else
		{
			Confirm();
		}
		void Confirm()
		{
			SavingCache.main.SaveWorldPersistent(revertSave, cache: true, saveRocketsAndBranches: true, addToRevert: false, deleteRevert: true);
			Base.sceneLoader.LoadBuildScene(askBuildNew: false);
		}
	}

	private void OpenClearDebrisMenu()
	{
		MenuGenerator.OpenConfirmation(CloseMode.Current, () => Loc.main.Clear_Debris_Warning, () => Loc.main.Clear_Debris_Confirm, ClearDebris);
		void ClearDebris()
		{
			for (int num = rockets.Count - 1; num >= 0; num--)
			{
				if (!rockets[num].hasControl.Value)
				{
					RocketManager.DestroyRocket(rockets[num], DestructionReason.Intentional);
				}
			}
			ScreenManager.main.CloseStack();
		}
	}

	public void ExitToBuild()
	{
		UpdatePersistent(cache: true, addToRevert: false, deleteRevert: false);
		Base.sceneLoader.LoadBuildScene(askBuildNew: true);
	}

	public void ExitToHub()
	{
		UpdatePersistent(cache: true, addToRevert: false, deleteRevert: false);
		Base.sceneLoader.LoadHubScene();
	}

	public void ExitToMainMenu()
	{
		UpdatePersistent(cache: true, addToRevert: false, deleteRevert: false);
		Base.sceneLoader.LoadHomeScene(null);
	}

	private void Update()
	{
		if (!(Time.timeScale < 0.01f))
		{
			timeSinceLastSave += Time.unscaledDeltaTime;
			if (timeSinceLastSave > 15f)
			{
				UpdatePersistent(cache: false, addToRevert: true, deleteRevert: false);
				timeSinceLastSave = 0f;
			}
		}
	}

	private void UpdatePersistent(bool cache, bool addToRevert, bool deleteRevert)
	{
		SavingCache.main.SaveWorldPersistent(CreateWorldSave(), cache, saveRocketsAndBranches: true, addToRevert, deleteRevert);
	}

	private WorldSave CreateWorldSave()
	{
		MapView.View view = Map.view.view;
		return new WorldSave(Application.version, CareerState.main.state, AstronautState.main.GetAstronautSave_Game(), new WorldSave.WorldState(WorldTime.main.worldTime, WorldTime.main.timewarpIndex, Map.manager.mapMode.Value, new Double3(view.position.Value.x, view.position.Value.y, view.distance), WorldAddress.GetMapAddress(view.target), WorldAddress.GetMapAddress(Map.navigation.target), WorldAddress.GetPlayerAddress(PlayerController.main.player.Value), PlayerController.main.cameraDistance), rockets.Select((Rocket rocket) => new RocketSave(rocket)).ToArray(), LogManager.main.branches, LogManager.main.completeLogs, LogManager.main.completeChallenges);
	}

	private void LoadPersistentAndLaunch()
	{
		MsgCollector logger = new MsgCollector();
		WorldSave worldSave = SavingCache.main.LoadWorldPersistent(logger, needsRocketsAndBranches: true, eraseCache: true);
		if (logger.msg.Length > 0)
		{
			ActionQueue.main.QueueMenu(Menu.read.Create(() => logger.msg.ToString()));
		}
		if (Base.sceneLoader.sceneSettings.launch)
		{
			LoadAndLaunch(worldSave);
			SavingCache.main.SaveRevertToLaunch(worldSave, cache: true);
		}
		else
		{
			LoadSave(worldSave, forLaunch: false, Menu.read);
		}
	}

	private void LoadAndLaunch(WorldSave save)
	{
		LoadSaveForLaunch(save, new MsgNone());
		if (SavingCache.main.TryLoadBuildPersistent(MsgDrawer.main, out var buildPersistent, eraseCache: false))
		{
			RocketManager.SpawnBlueprint(buildPersistent);
		}
		GameCamerasManager.main.InstantlyRotateCamera();
	}

	private void LoadSaveForLaunch(WorldSave save, I_MsgLogger logger)
	{
		save.state.playerAddress = "null";
		save.state.cameraDistance = 32f;
		save.state.timewarpPhase = 0;
		save.state.mapView = false;
		save.state.mapAddress = Base.planetLoader.spaceCenter.address;
		save.state.mapPosition = new Double3(Base.planetLoader.spaceCenter.LaunchPadLocation.position.x, Base.planetLoader.spaceCenter.LaunchPadLocation.position.y, Base.planetLoader.spaceCenter.LaunchPadLocation.position.y * 0.65);
		save.state.targetAddress = "null";
		LoadSave(save, forLaunch: true, logger);
	}

	public void LoadSave(WorldSave save, bool forLaunch, I_MsgLogger logger)
	{
		ClearWorld();
		CareerState.main.SetState(save.career);
		LogManager.main.branches = save.branches;
		LogManager.main.completeLogs = save.completeLogs ?? new HashSet<LogId>();
		LogManager.main.completeChallenges = save.completeChallenges ?? new HashSet<string>();
		WorldTime.main.worldTime = save.state.worldTime;
		WorldTime.main.SetTimewarpIndex_ForLoad(save.state.timewarpPhase);
		Location playerLocationFromSave = WorldAddress.GetPlayerLocationFromSave(save);
		WorldView.main.SetViewLocation(playerLocationFromSave);
		WorldView.main.viewDistance.Value = save.state.cameraDistance;
		AstronautState.main.state = new WorldSave.Astronauts(save.astronauts.astronauts, new List<WorldSave.Astronauts.Crew_World>(), null, null, save.astronauts.collectedRocks);
		bool flag = false;
		RocketSave[] array = save.rockets;
		foreach (RocketSave rocketSave in array)
		{
			if (!forLaunch || !WorldAddress.TryGetPlanetFromAddress(rocketSave.location.address, out var planetName) || !IsOnLaunchpad(planetName, rocketSave.location.position))
			{
				RocketManager.LoadRocket(rocketSave, out var hasNonOwnedParts);
				if (hasNonOwnedParts && !flag)
				{
					logger.Log("Save contains not owned parts");
					flag = true;
				}
			}
		}
		if (!DevSettings.DisableAstronauts)
		{
			WorldSave.Astronauts.EVA[] array2 = save.astronauts.eva.ToArray();
			foreach (WorldSave.Astronauts.EVA eVA in array2)
			{
				if (AstronautState.main.GetAstronautByName(eVA.astronautName).alive)
				{
					AstronautManager.main.SpawnEVA(eVA.astronautName, eVA.location.GetSaveLocation(save.state.worldTime), eVA.rotation, eVA.angularVelocity, eVA.ragdoll, eVA.fuelPercent, eVA.temperature).stats.Load(eVA.branch);
				}
			}
			foreach (WorldSave.Astronauts.Flag flag2 in save.astronauts.flags)
			{
				AstronautManager.main.SpawnFlag(flag2.location.GetSaveLocation(save.state.worldTime), flag2.direction);
			}
		}
		Map.manager.mapMode.Value = save.state.mapView;
		Map.view.view.target.Value = WorldAddress.GetMapInstance(save.state.mapAddress);
		Map.view.view.position.Value = (Double2)save.state.mapPosition;
		Map.view.view.distance.Value = save.state.mapPosition.z;
		Map.navigation.SetTarget(WorldAddress.GetMapInstance(save.state.targetAddress, allowNull: true));
		PlayerController.main.player.Value = WorldAddress.GetPlayerInstance(save.state.playerAddress);
		PlayerController.main.cameraDistance.Value = save.state.cameraDistance;
		GameCamerasManager.main.InstantlyRotateCamera();
		if (this.environment.environments != null)
		{
			Environment[] environments = this.environment.environments;
			foreach (Environment environment in environments)
			{
				if (environment.terrain != null)
				{
					environment.terrain.LoadFully();
				}
			}
		}
		LogManager.main.ClearBranches();
	}

	private void ClearWorld()
	{
		timeSinceLastSave = 0f;
		LogManager.main.branches = new Dictionary<int, Branch>();
		foreach (Rocket rocket in rockets)
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
		}
		while (rockets.Count > 0)
		{
			RocketManager.DestroyRocket(rockets[0], DestructionReason.Intentional);
		}
		while (AstronautManager.main.eva.Count > 0)
		{
			AstronautManager.DestroyEVA(AstronautManager.main.eva[0], death: false);
		}
		while (AstronautManager.main.flags.Count > 0)
		{
			AstronautManager.DestroyFlag(AstronautManager.main.flags[0]);
		}
		EffectManager.ClearEffects();
		PlayerController.main.ClearShakeEffects();
		PlayerController.main.cameraOffset.Value = Vector2.zero;
		PlayerController.main.switchOffset = Vector2.zero;
	}

	private static bool IsOnLaunchpad(string planet, Double2 position)
	{
		if (planet == Base.planetLoader.spaceCenter.address && Math.Abs((position - Base.planetLoader.spaceCenter.LaunchPadLocation.position).x) <= 30.0)
		{
			return Math.Abs((position - Base.planetLoader.spaceCenter.LaunchPadLocation.position).y) <= 200.0;
		}
		return false;
	}
}
