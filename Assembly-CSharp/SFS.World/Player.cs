using SFS.Input;
using SFS.Variables;
using UnityEngine;

namespace SFS.World;

public abstract class Player : ObservableMonoBehaviour
{
	public WorldLocation location;

	public MapPlayer mapPlayer;

	public Bool_Local isPlayer;

	public Bool_Local hasControl;

	public abstract float GetSizeRadius();

	public abstract void ClampTrackingOffset(ref Vector2 trackingOffset, float cameraDistance);

	public abstract bool OnInputEnd_AsPlayer(OnInputEndData data);

	public abstract float TryWorldSelect(TouchPosition data);

	public abstract bool CanTimewarp(I_MsgLogger logger, bool showSpeed);
}
