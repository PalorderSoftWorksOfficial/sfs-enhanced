using System.Collections;
using SFS.Input;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World;

public class Flag : Player
{
	public Transform holder;

	public MapIcon mapIcon;

	public WorldLoader loader;

	public int direction;

	private void Start()
	{
		holder.localScale = new Vector2(direction, 1f);
		holder.rotation = Quaternion.Euler(0f, 0f, (float)location.position.Value.AngleDegrees - 90f);
		mapIcon.SetRotation(holder.rotation.eulerAngles.z + 90f);
	}

	private void OnEnable()
	{
		AstronautManager.main.flags.Add(this);
	}

	private void OnDisable()
	{
		AstronautManager.main.flags.Remove(this);
	}

	public override float TryWorldSelect(TouchPosition data)
	{
		if (!holder.gameObject.activeSelf)
		{
			return float.PositiveInfinity;
		}
		return ((Vector2)holder.TransformPoint(Vector3.up) - data.World(0f)).magnitude - 0.5f;
	}

	public override bool CanTimewarp(I_MsgLogger logger, bool showSpeed)
	{
		return true;
	}

	public override void ClampTrackingOffset(ref Vector2 trackingOffset, float cameraDistance)
	{
		trackingOffset *= 0f;
	}

	public override float GetSizeRadius()
	{
		return 0f;
	}

	public override bool OnInputEnd_AsPlayer(OnInputEndData data)
	{
		return false;
	}

	public void ShowPlantAnimation()
	{
		StartCoroutine(AnimateFoldout());
	}

	private IEnumerator AnimateFoldout()
	{
		holder.localScale = new Vector2((float)direction * 0.25f, 0f);
		while (holder.localScale.y != 1f)
		{
			float y = holder.localScale.y;
			y += Time.fixedDeltaTime * 2f;
			y = Mathf.Clamp(y, -1f, 1f);
			holder.localScale = new Vector2(holder.localScale.x, y);
			yield return new WaitForFixedUpdate();
		}
		while (holder.localScale.x != (float)direction * 1f)
		{
			float x = holder.localScale.x;
			x += (float)direction * Time.fixedDeltaTime * 4f;
			x = Mathf.Clamp(x, -1f, 1f);
			holder.localScale = new Vector2(x, holder.localScale.y);
			yield return new WaitForFixedUpdate();
		}
	}
}
