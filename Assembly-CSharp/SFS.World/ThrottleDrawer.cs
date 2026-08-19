using System;
using SFS.Career;
using SFS.Input;
using SFS.Translations;
using SFS.UI;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public class ThrottleDrawer : MonoBehaviour
{
	public GameObject menuHolder;

	public TextAdapter throttleEnableText;

	public TextAdapter throttlePercentText;

	public Button toggleThrottleButton;

	public FillSlider throttleSlider;

	public Bool_Local shown = new Bool_Local();

	private Throttle_Local throttle = new Throttle_Local();

	private const float Pow = 1.6f;

	private void Start()
	{
		PlayerController.main.player.OnChange += new Action<Player>(OnPlayerChange);
		PlayerController.main.hasControl.OnChange += new Action(OnHasControlChange);
		throttle.OnChange += new Action<Throttle, Throttle>(OnThrottleChange);
		throttle.OnChange += new Action(UpdateShow);
		shown.OnChange += new Action(UpdateShow);
		WorldTime.main.realtimePhysics.OnChange += new Action(UpdateShow);
		CareerState.main.OnStateLoaded += UpdateShow;
		FillSlider fillSlider = throttleSlider;
		fillSlider.onSlide = (Action<float>)Delegate.Combine(fillSlider.onSlide, new Action<float>(UseUI_ToSetThrottle));
		Loc.OnChange += new Action(UpdateThrottleOnUI);
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Toggle_Ignition, ToggleThrottle);
		GameManager.AddOnKeyDown(KeybindingsPC.keys.MinMax_Throttle[0], delegate
		{
			SetThrottleRaw(0f);
		});
		GameManager.AddOnKeyDown(KeybindingsPC.keys.MinMax_Throttle[1], delegate
		{
			SetThrottleRaw(1f);
		});
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Throttle[0], delegate
		{
			CanThrottle(showMsg: true);
		});
		GameManager.AddOnKeyDown(KeybindingsPC.keys.Throttle[1], delegate
		{
			CanThrottle(showMsg: true);
		});
		GameManager.AddOnKey(KeybindingsPC.keys.Throttle[0], delegate
		{
			AdjustThrottleRaw(-0.5f * Time.deltaTime);
		});
		GameManager.AddOnKey(KeybindingsPC.keys.Throttle[1], delegate
		{
			AdjustThrottleRaw(0.5f * Time.deltaTime);
		});
	}

	private void OnDestroy()
	{
		PlayerController.main.hasControl.OnChange -= new Action(OnHasControlChange);
		throttle.OnChange -= new Action<Throttle, Throttle>(OnThrottleChange);
		throttle.OnChange -= new Action(UpdateShow);
		Loc.OnChange -= new Action(UpdateThrottleOnUI);
	}

	private void OnPlayerChange(Player player)
	{
		throttle.Value = ((player is Rocket rocket) ? rocket.throttle : null);
	}

	private void OnThrottleChange(Throttle oldValue, Throttle newValue)
	{
		if (oldValue != null)
		{
			oldValue.throttleOn.OnChange -= new Action(UpdateThrottleOnUI);
			oldValue.throttlePercent.OnChange -= new Action(UpdatePercentUI);
		}
		if (newValue != null)
		{
			newValue.throttleOn.OnChange += new Action(UpdateThrottleOnUI);
			newValue.throttlePercent.OnChange += new Action(UpdatePercentUI);
		}
	}

	private void UpdateShow()
	{
		if (!Base.sceneLoader.isUnloading)
		{
			menuHolder.SetActive(CareerState.main.HasFeature(WorldSave.CareerState.throttleFeature) && throttle.Value != null && shown.Value && (WorldTime.main.realtimePhysics.Value || true));
		}
	}

	private void UpdateThrottleOnUI()
	{
		if (!(throttle.Value == null))
		{
			((ButtonPC)toggleThrottleButton).SetSelected(throttle.Value.throttleOn.Value);
		}
	}

	private void UpdatePercentUI()
	{
		float value = throttle.Value.throttlePercent.Value;
		throttlePercentText.Text = value.ToPercentString();
		float t = ReverseCurve(value);
		t = Mathf.Lerp(0.16f, 0.84f, t);
		throttleSlider.SetFillAmount(t, invokeOnSlide: false);
	}

	private void OnHasControlChange()
	{
		throttlePercentText.Color = (PlayerController.main.hasControl.Value ? Color.white : new Color(1f, 1f, 1f, 0.4f));
		toggleThrottleButton.SetEnabled(PlayerController.main.hasControl.Value);
	}

	private void UseUI_ToSetThrottle(float newThrottleRaw)
	{
		SetThrottleRaw(newThrottleRaw);
		UpdatePercentUI();
	}

	public void ToggleThrottle()
	{
		if (CanThrottle(showMsg: true))
		{
			throttle.Value.throttleOn.Value = !throttle.Value.throttleOn;
		}
	}

	private void AdjustThrottleRaw(float delta)
	{
		if (CanThrottle(showMsg: false))
		{
			float num = ReverseCurve(throttle.Value.throttlePercent);
			num += delta;
			throttle.Value.throttlePercent.Value = Mathf.Clamp01(ApplyCurve(num));
		}
	}

	private void SetThrottleRaw(float newThrottleRaw)
	{
		if (CanThrottle(showMsg: true))
		{
			float value = ApplyCurve(newThrottleRaw);
			throttle.Value.throttlePercent.Value = Mathf.Clamp01(value);
		}
	}

	private bool CanThrottle(bool showMsg)
	{
		if (WorldTime.main.realtimePhysics.Value && throttle.Value != null)
		{
			PlayerController main = PlayerController.main;
			I_MsgLogger logger;
			if (!showMsg)
			{
				I_MsgLogger i_MsgLogger = new MsgNone();
				logger = i_MsgLogger;
			}
			else
			{
				I_MsgLogger i_MsgLogger = MsgDrawer.main;
				logger = i_MsgLogger;
			}
			return main.HasControl(logger);
		}
		return false;
	}

	private static float ApplyCurve(float a)
	{
		a = Mathf.Pow(a, 1.6f);
		if (float.IsNaN(a))
		{
			return 0f;
		}
		return a;
	}

	private static float ReverseCurve(float a)
	{
		a = Mathf.Pow(a, 0.625f);
		if (float.IsNaN(a))
		{
			return 0f;
		}
		return a;
	}
}
