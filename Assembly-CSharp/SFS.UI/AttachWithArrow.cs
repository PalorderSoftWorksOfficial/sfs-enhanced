using System;
using SFS.Builds;
using SFS.Cameras;
using SFS.Parts;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class AttachWithArrow : MonoBehaviour
{
	public enum Placement
	{
		Horizontal = -2,
		Vertical = -1,
		Left = 1,
		Right = 2,
		Top = 3,
		Bottom = 4
	}

	public RectTransform menuArea;

	public RectTransform menu;

	public RectTransform title;

	public RectTransform arrow;

	[Space]
	public Image arrowImage;

	public Image titleImage;

	public Image backgroundImage;

	[Space]
	public Placement preference = Placement.Horizontal;

	public Vector2 preferentialSpacing = new Vector2(0.15f, 0.15f);

	public float arrowSpacing = 25f;

	private Func<Vector2> getScreenPosition;

	private bool dontUpdateOnZoomChange;

	private float lastZoomDistance;

	private void Awake()
	{
		if (getScreenPosition == null)
		{
			base.enabled = false;
		}
	}

	public static Func<Vector2> FollowPart(Part part)
	{
		Part_Utility.GetBuildColliderBounds_WorldSpace(out var bounds, true, part);
		return FollowTransform(part.transform, part.transform.InverseTransformPoint(bounds.center));
	}

	public static Func<Vector2> FollowTransform(Transform target, Vector2 localPosition)
	{
		return () => ActiveCamera.Camera.camera.WorldToScreenPoint(target.TransformPoint(localPosition));
	}

	public void Open(Func<Vector2> getScreenPosition, bool dontUpdateOnZoomChange)
	{
		Close();
		this.getScreenPosition = getScreenPosition;
		base.enabled = true;
		this.dontUpdateOnZoomChange = dontUpdateOnZoomChange;
		if (dontUpdateOnZoomChange)
		{
			lastZoomDistance = BuildManager.main.buildCamera.CameraDistance;
		}
	}

	public void Close()
	{
		getScreenPosition = null;
		base.enabled = false;
	}

	private void LateUpdate()
	{
		if (dontUpdateOnZoomChange && lastZoomDistance != BuildManager.main.buildCamera.CameraDistance)
		{
			lastZoomDistance = BuildManager.main.buildCamera.CameraDistance;
			return;
		}
		UpdatePosition();
		UpdateArrowColor();
	}

	private void UpdatePosition()
	{
		Vector3 vector = menuArea.InverseTransformPoint(getScreenPosition());
		Placement bestPlacement = GetBestPlacement(vector);
		Rect rect = arrow.rect;
		var (num, num2, vector2) = GetPositionModifiers(bestPlacement, vector);
		menu.pivot = ((bestPlacement > Placement.Right) ? new Vector2(num2, num) : new Vector2(num, num2));
		Vector3 vector3 = ((bestPlacement > Placement.Right) ? new Vector3(0f, (0f - (num * 2f - 1f)) * rect.height * arrow.localScale.y, 0f) : new Vector3((0f - (num * 2f - 1f)) * rect.height * arrow.localScale.y, 0f, 0f));
		menu.localPosition = vector + vector3 + (Vector3)vector2;
		Vector3 eulerAngles = arrow.eulerAngles;
		eulerAngles.z = (new float[4] { 270f, 90f, 180f, 0f })[(int)(bestPlacement - 1)];
		arrow.eulerAngles = eulerAngles;
		arrow.localPosition = menu.InverseTransformPoint(menu.position);
	}

	private (float pivotFixed, float pivotVariable, Vector2 offsetRotation) GetPositionModifiers(Placement placement, Vector2 position)
	{
		Vector2 zero = Vector2.zero;
		float item;
		float num2;
		float num3;
		if (placement > Placement.Right)
		{
			item = ((placement != Placement.Top) ? 1 : 0);
			zero.y = ((placement == Placement.Top) ? 1 : (-1));
			float x = position.x;
			float num = menuArea.rect.width - position.x;
			float min = (arrow.rect.width + arrowSpacing) / 2f / menu.rect.width;
			float max = (menu.rect.width - (arrow.rect.width + arrowSpacing) / 2f) / menu.rect.width;
			num2 = Mathf.Clamp(x / menu.rect.width, min, max);
			num3 = Mathf.Clamp(1f - num / menu.rect.width, min, max);
			if (num2 > num3)
			{
				float num4 = num3;
				num3 = num2;
				num2 = num4;
			}
		}
		else
		{
			item = ((placement == Placement.Left) ? 1 : 0);
			zero.x = ((placement != Placement.Left) ? 1 : (-1));
			float num5 = menuArea.rect.height - position.y;
			float y = position.y;
			float min2 = (arrow.rect.height + arrowSpacing) / 2f / menu.rect.height;
			float max2 = (menu.rect.height - (arrow.rect.height + arrowSpacing) / 2f) / menu.rect.height;
			num2 = Mathf.Clamp(1f - num5 / menu.rect.height, min2, max2);
			num3 = Mathf.Clamp(y / menu.rect.height, min2, max2);
		}
		float item2 = Mathf.Clamp(0.5f, num2, num3);
		return (pivotFixed: item, pivotVariable: item2, offsetRotation: zero);
	}

	private Placement GetBestPlacement(Vector2 position)
	{
		Rect screenRect = menuArea.rect;
		Rect menuRect = menu.rect;
		Vector2 spacing = preferentialSpacing * menuRect.size;
		Placement? output = null;
		TryUsePlacement(preference + 3);
		TryUsePlacement(preference + 4);
		if (GetPlacementScore(output) < 0.0)
		{
			for (int i = 0; i < 4; i++)
			{
				TryUsePlacement((Placement)(1 + i));
			}
		}
		return output.Value;
		Vector2 GetAttachmentLeftover(Placement p)
		{
			return p switch
			{
				Placement.Left => new Vector2(position.x - menu.rect.width, menuArea.rect.height - menuRect.height), 
				Placement.Right => new Vector2(menuArea.rect.width - position.x - menu.rect.width, screenRect.height - menuRect.height), 
				Placement.Top => new Vector2(menuArea.rect.width - menu.rect.width, screenRect.height - position.y - menuRect.height), 
				Placement.Bottom => new Vector2(menuArea.rect.width - menu.rect.width, position.y - menuRect.height), 
				_ => throw new Exception(), 
			};
		}
		double GetPlacementScore(Placement? p)
		{
			if (!p.HasValue)
			{
				return double.MinValue;
			}
			double num = 0.0;
			Vector2 vector = GetAttachmentLeftover(p.Value);
			if (vector.x - spacing.x < 0f)
			{
				num -= (double)Mathf.Max(1f, Mathf.Abs((vector.x - spacing.x) / spacing.x));
			}
			if (vector.y - spacing.y < 0f)
			{
				num -= (double)Mathf.Max(1f, Mathf.Abs((vector.y - spacing.y) / spacing.y));
			}
			if (p.Value > Placement.Right)
			{
				if (position.x < spacing.x)
				{
					num--;
				}
				if (screenRect.width - position.x < spacing.x)
				{
					num--;
				}
			}
			else
			{
				if (position.y < spacing.y)
				{
					num--;
				}
				if (screenRect.height - position.y < spacing.y)
				{
					num--;
				}
			}
			return num;
		}
		void TryUsePlacement(Placement? p)
		{
			if (GetPlacementScore(output) < GetPlacementScore(p))
			{
				output = p;
			}
		}
	}

	private void UpdateArrowColor()
	{
		bool flag = title.gameObject.activeSelf && arrow.eulerAngles.z == 0f;
		arrowImage.color = (flag ? titleImage : backgroundImage).color;
		arrowImage.material = (flag ? titleImage : backgroundImage).material;
	}
}
