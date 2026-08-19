using UnityEngine;

namespace SFS.Core;

public static class Links
{
	public static void OpenAppStore(bool closeApplication)
	{
		Application.OpenURL((Application.platform == RuntimePlatform.Android) ? "https://play.google.com/store/apps/details?id=com.StefMorojna.SpaceflightSimulator&hl=nl" : "https://apps.apple.com/nl/app/spaceflight-simulator/id1308057272");
		if (closeApplication)
		{
			Application.Quit();
		}
	}
}
