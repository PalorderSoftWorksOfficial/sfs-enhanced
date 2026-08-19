using System.Collections.Generic;
using UnityEngine;

namespace SFS.Platform;

public class PlatformManager : MonoBehaviour
{
	public const PlatformType current = PlatformType.PC;

	public List<PlatformSpecific> allPlatformSpecific;

	private void Awake()
	{
		foreach (PlatformSpecific item in allPlatformSpecific)
		{
			bool active = item.platformType == PlatformType.PC;
			foreach (GameObject item2 in item.affiliatedGameObject)
			{
				item2.SetActive(active);
			}
		}
	}
}
