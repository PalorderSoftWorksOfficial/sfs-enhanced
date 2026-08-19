using System;
using System.Collections.Generic;
using UnityEngine;

namespace SFS.Platform;

[Serializable]
public class PlatformSpecific
{
	public PlatformType platformType;

	public List<GameObject> affiliatedGameObject;
}
