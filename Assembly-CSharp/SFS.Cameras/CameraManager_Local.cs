using System;
using SFS.Variables;

namespace SFS.Cameras;

[Serializable]
public class CameraManager_Local : Obs_Destroyable<CameraManager>
{
	protected override bool IsEqual(CameraManager a, CameraManager b)
	{
		return a == b;
	}
}
