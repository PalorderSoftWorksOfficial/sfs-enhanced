using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFS.Input;
using SFS.IO;
using SFS.Translations;
using SFS.World;
using SFS.WorldBase;
using UnityEngine;

namespace SFS.UI;

public class CreateWorldMenu : BasicMenu
{
	public WorldsMenu worldsMenu;

	public Button createWorldButton;

	public TextBoxAdapter worldNameField;

	public GameObject solarSystemSelectorHolder;

	public TextAdapter solarSystemButtonText;

	public ButtonGroup difficultyGroup;

	public ButtonGroup modeGroup;

	public ToggleButton quicksavesToggle;

	public GameObject togglesHolder;

	public TextAdapter difficultyInfo;

	private static readonly List<WorldMode.Mode> ModeIndices = new List<WorldMode.Mode>
	{
		WorldMode.Mode.Sandbox,
		WorldMode.Mode.Challenge
	};

	private static readonly List<Difficulty> DifficultyIndices = new List<Difficulty>
	{
		Difficulty.Normal,
		Difficulty.Hard,
		Difficulty.Realistic
	};

	private string solarSystemName;

	private bool quicksaves;

	private void Start()
	{
		menuHolder.gameObject.SetActive(value: false);
		quicksavesToggle.Bind(ToggleQuicksaves, () => quicksaves);
	}

	public override void Open()
	{
		worldNameField.Text = Loc.main.Default_World_Name;
		solarSystemSelectorHolder.SetActive(FileLocations.SolarSystemsFolder.GetFoldersInFolder(recursively: false).Any((FolderPath a) => a.FolderName != "Example"));
		SetSolarSystem("");
		modeGroup.SetSelectedIndex(0);
		quicksaves = true;
		quicksavesToggle.UpdateUI(instantAnimation: true);
		difficultyGroup.SetSelectedIndex(0);
		base.Open();
	}

	public override void Close()
	{
		worldNameField.Close();
		base.Close();
	}

	private void SetSolarSystem(string a)
	{
		solarSystemName = a;
		solarSystemButtonText.Text = ((a == "") ? ((string)Loc.main.Default_Solar_System) : a);
	}

	public void OpenSelectSolarSystemMenu()
	{
		List<MenuElement> buttons = new List<MenuElement>
		{
			TextBuilder.CreateText(() => Loc.main.Select_Solar_System),
			ElementGenerator.VerticalSpace(50),
			new SizeSyncerBuilder(out var horizontal)
		};
		AddButton(Loc.main.Default_Solar_System, "");
		if (DevSettings.FullVersion && FileLocations.SolarSystemsFolder.FolderExists())
		{
			foreach (FolderPath item in FileLocations.SolarSystemsFolder.GetFoldersInFolder(recursively: false))
			{
				if (item.FolderName != "Example")
				{
					AddButton(Loc.main.Custom_Solar_System.Inject(item.FolderName, "name"), item.FolderName);
				}
			}
		}
		MenuGenerator.OpenMenu(CancelButton.Cancel, CloseMode.Current, buttons.ToArray());
		void AddButton(string text, string solarSystemName)
		{
			buttons.Add(ButtonBuilder.CreateButton(horizontal, () => text, delegate
			{
				SetSolarSystem(solarSystemName);
			}, CloseMode.Current));
		}
	}

	private void ToggleQuicksaves()
	{
		quicksaves = !quicksaves;
	}

	public void OnDifficultyCycle()
	{
		string text = "";
		if (DifficultyIndices.IsValidIndex(difficultyGroup.SelectedIndex))
		{
			Difficulty difficulty = DifficultyIndices[difficultyGroup.SelectedIndex];
			text += Loc.main.Difficulty_Scale_Stat.Inject((20.0 / difficulty.DefaultRadiusScale).ToString(CultureInfo.InvariantCulture), "scale");
			text = text + "\n" + Loc.main.Difficulty_Isp_Stat.Inject(difficulty.IspMultiplier.ToString(CultureInfo.InvariantCulture), "value");
			text = text + "\n" + Loc.main.Difficulty_Dry_Mass_Stat.Inject(difficulty.DryMassMultiplier.ToString(CultureInfo.InvariantCulture), "value");
			text = text + "\n" + Loc.main.Difficulty_Engine_Mass_Stat.Inject(difficulty.EngineMassMultiplier.ToString(CultureInfo.InvariantCulture), "value");
		}
		difficultyInfo.Text = text;
		UpdateCanCreate();
	}

	public void OnModeCycle()
	{
		togglesHolder.SetActive(ModeIndices.IsValidIndex(modeGroup.SelectedIndex) && ModeIndices[modeGroup.SelectedIndex] == WorldMode.Mode.Challenge);
		UpdateCanCreate();
	}

	private void UpdateCanCreate()
	{
		createWorldButton.SetEnabled(modeGroup.SelectedIndex != -1 && difficultyGroup.SelectedIndex != -1);
	}

	public void CreateWorld()
	{
		if (!FileLocations.WorldsFolder.FolderExists())
		{
			FileLocations.WorldsFolder.CreateFolder();
		}
		string text = ((worldNameField.Text == "") ? ((string)Loc.main.Default_World_Name) : worldNameField.Text);
		text = PathUtility.MakeUsable(text, Loc.main.Default_World_Name);
		text = PathUtility.AutoNameExisting(text, FileLocations.WorldsFolder);
		WorldReference worldReference = new WorldReference(text);
		SolarSystemReference solarSystem = new SolarSystemReference(solarSystemName);
		Difficulty difficulty = DifficultyIndices[difficultyGroup.SelectedIndex];
		WorldMode worldMode = new WorldMode(ModeIndices[modeGroup.SelectedIndex]);
		worldMode.allowQuicksaves = worldMode.mode == WorldMode.Mode.Sandbox || quicksaves;
		int num;
		if (text.ToLower() == "test career")
		{
			num = 0;
			if (num != 0)
			{
				worldMode.mode = WorldMode.Mode.Career;
			}
		}
		else
		{
			num = 0;
		}
		WorldSettings settings = new WorldSettings(solarSystem, difficulty, worldMode, new WorldPlaytime(DateTime.Now.Ticks, 0.0), new SandboxSettings.Data());
		worldReference.SaveWorldSettings(settings);
		Close();
		worldsMenu.OnWorldCreated(text);
		if (num != 0)
		{
			Menu.read.Open(() => "Career mode is currently a very minimal prototype with only a few missions and unlocks\n\nIf this prototype gets positive feedback, we will add tons of missions, unlocks and polish it to perfection\n\n- Spaceflight Simulator developer team");
		}
	}
}
