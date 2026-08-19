using System;
using SFS.Cameras;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class GameSelector : MonoBehaviour
{
	[Serializable]
	public class Selectable_Local : Obs_Destroyable<SelectableObject>
	{
		protected override bool IsEqual(SelectableObject a, SelectableObject b)
		{
			return a == b;
		}
	}

	public static GameSelector main;

	public RectTransform menuHolder;

	public TextAdapter selectedName;

	public IconButton navigateButton;

	public IconButton focusButton;

	public IconButton switchButton;

	public IconButton renameButton;

	public IconButton endMissionButton;

	public Bool_Local showMenuInWorldMode;

	public Selectable_Local selected_World;

	public Selectable_Local selected_Map;

	public Selectable_Local selected;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		selected_World.OnChange += new Action(UpdateSelected);
		selected_Map.OnChange += new Action(UpdateSelected);
		Map.manager.mapMode.OnChange += new Action(UpdateSelected);
		showMenuInWorldMode.OnChange += new Action(UpdateSelected);
		selected.OnChange += new Action(OnSelectedChange);
		navigateButton.button.onClick += new Action(Target);
		focusButton.button.onClick += new Action(Focus);
		switchButton.button.onClick += new Action(SwitchTo);
		renameButton.button.onClick += new Action(Rename);
		endMissionButton.button.onClick += new Action(EndMission);
		PlayerController.main.player.OnChange += (Action)delegate
		{
			if (selected_World.Value is MapPlayer mapPlayer && !mapPlayer.Player.isPlayer)
			{
				selected_World.Value = null;
			}
		};
		WorldView.main.viewDistance.OnChange += (Action)delegate
		{
			showMenuInWorldMode.Value = WorldView.main.viewDistance.Value < 5000f;
		};
	}

	private void Target()
	{
		Map.navigation.ToggleTarget(selected.Value);
		CloseMenu();
	}

	private void Focus()
	{
		Map.view.ToggleFocus(selected.Value);
		CloseMenu();
	}

	private void SwitchTo()
	{
		if (!(selected.Value == null) && selected.Value is MapPlayer mapPlayer && !mapPlayer.Player.isPlayer.Value)
		{
			if (Map.manager.mapMode.Value)
			{
				Map.view.SetViewSmooth(new MapView.View(mapPlayer.Location.planet.mapPlanet, mapPlayer.Location.position, (double)Map.view.view.distance * 0.800000011920929));
				PlayerController.main.player.Value = mapPlayer.Player;
			}
			else
			{
				PlayerController.main.SmoothChangePlayer(mapPlayer.Player);
			}
			CloseMenu();
		}
	}

	private void Rename()
	{
		SelectableObject currentSelected = selected.Value;
		if (currentSelected != null)
		{
			Menu.textInput.Open(Loc.main.Cancel, Loc.main.Rename, delegate(string[] input)
			{
				ApplyRename(input[0]);
			}, CloseMode.Current, TextInputMenu.Element(string.Empty, selected.Value.Select_DisplayName));
		}
		CloseMenu();
		void ApplyRename(string input)
		{
			currentSelected.Select_DisplayName = input;
			OnSelectedChange();
		}
	}

	private void EndMission()
	{
		SelectableObject value = selected.Value;
		if (!(value is MapPlayer mapPlayer))
		{
			if (value is RockSelector rockSelector)
			{
				rockSelector.CollectRock();
			}
		}
		else
		{
			mapPlayer.OnEndMission();
		}
	}

	private void CloseMenu()
	{
		if (Map.manager.mapMode.Value)
		{
			selected_Map.Value = null;
		}
		else
		{
			selected_World.Value = null;
		}
	}

	private void UpdateSelected()
	{
		selected.Value = (Map.manager.mapMode.Value ? selected_Map.Value : (showMenuInWorldMode.Value ? selected_World.Value : null));
	}

	private void OnSelectedChange()
	{
		if (Base.sceneLoader.isUnloading)
		{
			return;
		}
		SelectableObject value = selected.Value;
		bool flag = (base.enabled = value != null);
		menuHolder.gameObject.SetActive(flag);
		if (flag)
		{
			selectedName.Text = value.Select_DisplayName;
			navigateButton.Show = value.Select_CanNavigate && (!(value is MapPlayer mapPlayer) || !mapPlayer.Player.isPlayer);
			navigateButton.text.Text = (value.IsNavigationTarget() ? Loc.main.End_Navigation : Loc.main.Navigate_To);
			focusButton.Show = (bool)Map.manager.mapMode && value.Select_CanFocus;
			switchButton.Show = value is MapPlayer mapPlayer2 && !mapPlayer2.Player.isPlayer;
			renameButton.Show = value.Select_CanRename;
			renameButton.button.SetEnabled(enabled: true);
			endMissionButton.Show = value.Select_CanEndMission;
			endMissionButton.Text = value.Select_EndMissionText ?? "";
			if (value.Select_EndMissionIcon != null)
			{
				endMissionButton.Icon = value.Select_EndMissionIcon;
			}
			endMissionButton.button.SetEnabled(enabled: true);
		}
	}

	private void LateUpdate()
	{
		SelectableObject value = selected.Value;
		menuHolder.position = (Vector2)ActiveCamera.main.activeCamera.Value.camera.WorldToScreenPoint(value.Select_MenuPosition);
		string text = ((!(value is MapPlanet mapPlanet)) ? ((string)(value.IsViewTarget() ? Loc.main.Stop_Tracking : Loc.main.Track)) : ((string)((Map.view.view.distance.Value < mapPlanet.FocusDistance) ? Loc.main.Unfocus : Loc.main.Focus)));
		focusButton.text.Text = text;
		Player player = PlayerController.main.player.Value;
		if (!(value is MapFlag mapFlag))
		{
			if (!(value is RockSelector rockSelector))
			{
				if (!(value is MapAstronaut mapAstronaut))
				{
					if (value is MapRocket mapRocket)
					{
						bool flag = !(player is Rocket) && !Map.manager.mapMode.Value && !MapRocket.CanRecover(mapRocket.rocket, checkAchievements: false);
						endMissionButton.button.SetEnabled(!flag);
					}
				}
				else
				{
					endMissionButton.button.SetEnabled(MapAstronaut.CanRecover(mapAstronaut.astronaut));
				}
			}
			else
			{
				endMissionButton.button.SetEnabled(IsAstronautInRange(rockSelector.Location.position));
			}
		}
		else
		{
			endMissionButton.button.SetEnabled(IsAstronautInRange(mapFlag.Player.location.position.Value));
		}
		bool IsAstronautInRange(Double2 a)
		{
			if (player is Astronaut_EVA astronaut_EVA)
			{
				return Vector2.Distance(astronaut_EVA.location.position.Value, a) < 6f;
			}
			return false;
		}
	}
}
