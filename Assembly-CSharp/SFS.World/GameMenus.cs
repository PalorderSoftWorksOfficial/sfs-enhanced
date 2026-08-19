using System;
using SFS.UI;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class GameMenus : MonoBehaviour
{
	public static GameMenus main;

	public Button recoverButton;

	public GameMenuLayout_Mobile menuLayoutMobile;

	private float showRecoverButton;

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_100Ms = (Action)Delegate.Combine(timeEvent.on_100Ms, new Action(UpdateTopCenter));
	}

	private void OnDestroy()
	{
		TimeEvent timeEvent = TimeEvent.main;
		timeEvent.on_100Ms = (Action)Delegate.Remove(timeEvent.on_100Ms, new Action(UpdateTopCenter));
	}

	private void UpdateTopCenter()
	{
		if (CanRecover())
		{
			showRecoverButton += 0.1f;
		}
		else
		{
			showRecoverButton = 0f;
		}
		bool flag = showRecoverButton > 2f;
		if (recoverButton.gameObject.activeSelf != flag)
		{
			recoverButton.gameObject.SetActive(flag);
		}
	}

	private bool CanRecover()
	{
		Player value = PlayerController.main.player.Value;
		if (value is Rocket rocket)
		{
			return MapRocket.CanRecover(rocket, checkAchievements: true);
		}
		if (value is Astronaut_EVA astronaut)
		{
			return MapAstronaut.CanRecover(astronaut);
		}
		return false;
	}
}
