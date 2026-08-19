using SFS.Variables;
using SFS.World;
using UnityEngine;

namespace SFS.Builds;

public class GridSize : MonoBehaviour
{
	public Vector2_Local size;

	public float centerX;

	public Sprite newBackgroundTest;

	public SpriteRenderer gridSprite;

	public SpriteRenderer centerMarkerSprite;

	public SpriteRenderer border_Left;

	public SpriteRenderer border_Right;

	public SpriteRenderer border_Bottom;

	public SpriteRenderer border_Top;

	private Vector2 sizeForInfinite = new Vector2(250f, 120f) * 2f;

	public void Initialize()
	{
		UpdateBuildSpaceSize();
		if (!DevSettings.DisableNewBuild)
		{
			gridSprite.sprite = newBackgroundTest;
		}
	}

	public void UpdateBuildSpaceSize()
	{
		size.Value = GetOwnedGridSize(returnZeroIfInfinite: true);
		OnSizeChange();
		centerX = GetOwnedGridSize(returnZeroIfInfinite: false).x * 0.5f;
	}

	public static Vector2 GetOwnedGridSize(bool returnZeroIfInfinite)
	{
		if (SandboxSettings.main.settings.infiniteBuildArea & returnZeroIfInfinite)
		{
			return Vector2.zero;
		}
		if (!DevSettings.FullVersion)
		{
			return new Vector2(14f, 35f);
		}
		return new Vector2(20f, 100f);
	}

	private void OnSizeChange()
	{
		if (SandboxSettings.main.settings.infiniteBuildArea)
		{
			gridSprite.size = sizeForInfinite;
			centerMarkerSprite.transform.localScale = new Vector3(centerMarkerSprite.transform.localScale.x, sizeForInfinite.y);
			Update();
		}
		else
		{
			gridSprite.size = size.Value;
			gridSprite.transform.position = Vector3.zero;
			centerMarkerSprite.transform.localScale = new Vector3(centerMarkerSprite.transform.localScale.x, size.Value.y);
			centerMarkerSprite.transform.position = new Vector3(size.Value.x * 0.5f, 0f);
		}
		border_Left.transform.position = new Vector3(-50f, size.Value.y / 2f);
		border_Right.transform.position = new Vector3(size.Value.x + 50f, size.Value.y / 2f);
		border_Bottom.transform.position = new Vector3(size.Value.x / 2f, -50f);
		border_Top.transform.position = new Vector3(size.Value.x / 2f, size.Value.y + 50f);
		Transform obj = border_Left.transform;
		Vector3 localScale = (border_Right.transform.localScale = new Vector3(100f, size.Value.y));
		obj.localScale = localScale;
		Transform obj2 = border_Bottom.transform;
		localScale = (border_Top.transform.localScale = new Vector3(size.Value.x + 200f, 100f));
		obj2.localScale = localScale;
		bool active = !SandboxSettings.main.settings.infiniteBuildArea;
		border_Left.gameObject.SetActive(active);
		border_Right.gameObject.SetActive(active);
		border_Bottom.gameObject.SetActive(active);
		border_Top.gameObject.SetActive(active);
	}

	public bool InsideGrid(Vector2 worldPosition, float threshold)
	{
		if (SandboxSettings.main.settings.infiniteBuildArea)
		{
			return true;
		}
		Vector2 vector = base.transform.InverseTransformPoint(worldPosition);
		if (vector.x > 0f - threshold && vector.y > 0f - threshold && vector.x < size.Value.x + threshold)
		{
			return vector.y < size.Value.y + threshold;
		}
		return false;
	}

	private void Update()
	{
		if (SandboxSettings.main.settings.infiniteBuildArea)
		{
			gridSprite.transform.position = (BuildManager.main.buildCamera.CameraPosition - sizeForInfinite / 2f).Round(1f);
			centerMarkerSprite.transform.position = new Vector3(centerX, (BuildManager.main.buildCamera.CameraPosition - sizeForInfinite / 2f).Round(1f).y);
		}
	}
}
