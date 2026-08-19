using System.Linq;
using SFS.Parts.Modules;
using SFS.World;
using UnityEngine;
using UnityEngine.Analytics;

namespace SFS.Tutorials;

public class Tutorial_World : Tutorial_Base
{
	public GameObject usePartPopup;

	public GameObject ignitionPopup;

	public GameObject throttlePopup;

	public GameObject retryPopup;

	private void Start()
	{
		usePartPopup.SetActive(value: false);
		ignitionPopup.SetActive(value: false);
		throttlePopup.SetActive(value: false);
		retryPopup.SetActive(value: false);
		if (FileLocations.HasNotification("Tut_Launch"))
		{
			return;
		}
		if (!FileLocations.HasNotification("First_Time_Playing"))
		{
			FileLocations.WriteNotification("Tut_Launch");
		}
		else
		{
			if (AnalyticsSessionInfo.sessionCount > 3 && !Application.isEditor)
			{
				return;
			}
			Player_Local p = PlayerController.main.player;
			Add_ShowPopup(throttlePopup, () => p.Value is Rocket rocket && rocket.throttle.throttlePercent.Value > 0.95f);
			Add_ShowPopup(usePartPopup, () => p.Value is Rocket rocket && (rocket.partHolder.GetModules<EngineModule>().Any((EngineModule a) => a.engineOn.Value || !a.source.CanFlow(new MsgNone())) || !rocket.partHolder.HasModule<EngineModule>()));
			Add_ShowPopup(ignitionPopup, () => p.Value is Rocket rocket && rocket.throttle.throttleOn.Value);
			float liftoffTime = Time.unscaledTime;
			Add_Action(delegate
			{
				liftoffTime = Time.unscaledTime;
			});
			Add_Check(delegate
			{
				if (p.Value is Rocket rocket && rocket.location.velocity.Value.magnitude > 10.0)
				{
					EngineModule[] modules = rocket.partHolder.GetModules<EngineModule>();
					for (int i = 0; i < modules.Length; i++)
					{
						modules[i].ISP.Value = 70f;
					}
					return true;
				}
				if (Time.unscaledTime - liftoffTime > 5f)
				{
					retryPopup.SetActive(value: true);
				}
				return false;
			});
			Add_Action(delegate
			{
				FileLocations.WriteNotification("Tut_Launch");
			});
		}
	}
}
