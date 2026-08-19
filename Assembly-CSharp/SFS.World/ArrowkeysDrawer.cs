using System;
using SFS.Cameras;
using SFS.Input;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class ArrowkeysDrawer : MonoBehaviour
{
	public GameObject menuHolder;

	public MoveModule arrowkeysAnimation;

	public MoveModule turnkeysAnimation;

	public Button turnLeft;

	public Button turnRight;

	public Button left;

	public Button right;

	public Button up;

	public Button down;

	public Button rcsButton;

	private Arrowkeys_Local arrowkeys = new Arrowkeys_Local();

	private Float_Local turn_Axis = new Float_Local();

	private Float_Local x_Axis = new Float_Local();

	private Float_Local y_Axis = new Float_Local();

	public Transform arrowTransform;

	private void Start()
	{
		PlayerController.main.player.OnChange += new Action(OnPlayerChange);
		PlayerController.main.hasControl.OnChange += new Action(OnHasControlChange);
		arrowkeys.OnChange += new Action<Arrowkeys, Arrowkeys>(OnArrowkeysChange);
		turn_Axis.OnChange += new Action(PushTurnAxis);
		PlayerController.main.hasControl.OnChange += new Action(PushTurnAxis);
		WorldTime.main.realtimePhysics.OnChange += new Action(PushTurnAxis);
		x_Axis.OnChange += new Action(PushDirectionalAxis);
		y_Axis.OnChange += new Action(PushDirectionalAxis);
		PlayerController.main.hasControl.OnChange += new Action(PushDirectionalAxis);
		WorldTime.main.realtimePhysics.OnChange += new Action(PushDirectionalAxis);
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Toggle_RCS, ToggleRCS);
		GameManager.AddAxis((KeybindingsPC.keys.Turn_Rocket[0], KeybindingsPC.keys.Turn_Rocket[1]), ref turn_Axis);
		GameManager.AddAxis((KeybindingsPC.keys.Move_Rocket_Using_RCS[1], KeybindingsPC.keys.Move_Rocket_Using_RCS[3]), ref x_Axis);
		GameManager.AddAxis((KeybindingsPC.keys.Move_Rocket_Using_RCS[2], KeybindingsPC.keys.Move_Rocket_Using_RCS[0]), ref y_Axis);
		rcsButton.onClick += new Action(ToggleRCS);
		static void ToggleRCS()
		{
			if (PlayerController.main.player.Value is Rocket rocket)
			{
				if (rocket.partHolder.HasModule<RcsModule>())
				{
					rocket.partHolder.GetModules<RcsModule>()[0].ToggleRCS(new UsePartData(new UsePartData.SharedData(fromStaging: false), null));
				}
				else
				{
					MsgDrawer.main.Log("Your rocket has no RCS thrusters");
				}
			}
		}
	}

	private void OnPlayerChange()
	{
		Player value = PlayerController.main.player.Value;
		arrowkeys.Value = ((value is Rocket rocket) ? rocket.arrowkeys : ((value is Astronaut_EVA astronaut_EVA) ? astronaut_EVA.arrowkeys : null));
	}

	private void OnArrowkeysChange(Arrowkeys oldValue, Arrowkeys newValue)
	{
		if (oldValue != null)
		{
			oldValue.hasTurn.OnChange -= new Action(UpdateTurnAnimation);
			oldValue.rcs.OnChange -= new Action(UpdateArrowkeysAnimation);
		}
		if (newValue != null)
		{
			newValue.hasTurn.OnChange += new Action(UpdateTurnAnimation);
			newValue.rcs.OnChange += new Action(UpdateArrowkeysAnimation);
		}
	}

	private void UpdateShow()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			menuHolder.SetActive(arrowkeys.Value != null && WorldTime.main.realtimePhysics.Value);
		}
	}

	private void UpdateTurnAnimation()
	{
	}

	private void UpdateArrowkeysAnimation()
	{
		((ButtonPC)rcsButton).SetSelected(arrowkeys.Value.rcs.Value);
	}

	private void OnHasControlChange()
	{
		rcsButton.SetEnabled(PlayerController.main.hasControl.Value);
	}

	private void PushTurnAxis()
	{
		if (arrowkeys.Value == null)
		{
			return;
		}
		if (!PlayerController.main.hasControl)
		{
			if (turn_Axis.Value != 0f)
			{
				MsgDrawer.main.Log(Loc.main.No_Control_Msg);
			}
			arrowkeys.Value.turnAxis.Value = 0f;
		}
		else if (!WorldTime.main.realtimePhysics)
		{
			if (turn_Axis.Value != 0f)
			{
				MsgDrawer.main.Log(Loc.main.Cannot_Turn_While_Timewarping);
			}
			arrowkeys.Value.turnAxis.Value = 0f;
		}
		else
		{
			arrowkeys.Value.turnAxis.Value = turn_Axis;
		}
	}

	private void PushDirectionalAxis()
	{
		if (arrowkeys.Value == null)
		{
			return;
		}
		if (!PlayerController.main.hasControl)
		{
			if (Reset())
			{
				MsgDrawer.main.Log(Loc.main.No_Control_Msg);
			}
		}
		else if (!WorldTime.main.realtimePhysics)
		{
			if (Reset())
			{
				MsgDrawer.main.Log(Loc.main.Cannot_Turn_While_Timewarping);
			}
		}
		else
		{
			arrowkeys.Value.rawArrowkeysAxis.Value = new Vector2(x_Axis, y_Axis);
			arrowkeys.Value.horizontalAxis.Value = new Double2((float)x_Axis, 0.0).Rotate((float)ActiveCamera.Camera.rotation * ((float)Math.PI / 180f));
			arrowkeys.Value.verticalAxis.Value = new Double2(0.0, (float)y_Axis).Rotate((float)ActiveCamera.Camera.rotation * ((float)Math.PI / 180f));
		}
		bool Reset()
		{
			if (new Vector2(x_Axis, y_Axis) == Vector2.zero)
			{
				return false;
			}
			arrowkeys.Value.rawArrowkeysAxis.Value = Vector2.zero;
			arrowkeys.Value.horizontalAxis.Value = Vector2.zero;
			arrowkeys.Value.verticalAxis.Value = Vector2.zero;
			return true;
		}
	}
}
