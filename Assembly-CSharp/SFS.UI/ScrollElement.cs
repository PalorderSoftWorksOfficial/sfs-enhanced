using System;
using SFS.Input;
using UnityEngine;

namespace SFS.UI;

public class ScrollElement : MonoBehaviour
{
	public enum ClampType
	{
		None,
		Window,
		Mask
	}

	public Button button;

	[Space]
	public bool horizontal;

	public bool vertical;

	[Space]
	public ClampType clampType;

	public int border;

	[Space]
	public bool resetPositionAtEnable = true;

	public Vector2 startPivot = new Vector2(0.5f, 0.5f);

	public Vector2 backupPivot = new Vector2(0.5f, 0.5f);

	[Space]
	public float momentumDecayTime = 0.1f;

	private Vector2 velocity;

	[HideInInspector]
	public int holding;

	private int update;

	private float rubberbandVelocityX;

	private float rubberbandVelocityY;

	public Vector2 FreeMoveSpace
	{
		get
		{
			if (clampType == ClampType.Window)
			{
				return GetParentRect().rect.size - Vector2.one * border - GetRect().rect.size;
			}
			if (clampType == ClampType.Mask)
			{
				return GetRect().rect.size - Vector2.one * border - GetParentRect().rect.size;
			}
			return Vector2.zero;
		}
	}

	public Vector2 PercentPosition
	{
		get
		{
			Vector2 result = Vector2.zero;
			if (clampType == ClampType.Window)
			{
				result = (GetRect().GetLocalPosition() - Vector2.one * border) / FreeMoveSpace;
			}
			if (clampType == ClampType.Mask)
			{
				result = (-GetRect().GetLocalPosition() - Vector2.one * border) / FreeMoveSpace;
			}
			if (FreeMoveSpace.x == 0f)
			{
				result.x = 0f;
			}
			if (FreeMoveSpace.y == 0f)
			{
				result.y = 0f;
			}
			return result;
		}
		set
		{
			if (clampType == ClampType.Window)
			{
				GetRect().SetPosition(value * FreeMoveSpace);
			}
			if (clampType == ClampType.Mask)
			{
				GetRect().SetPosition(value * -FreeMoveSpace);
			}
		}
	}

	private Vector2 RubberbandDistance => new Vector2(100f, 100f) / FreeMoveSpace;

	private void Start()
	{
		if (button != null)
		{
			RegisterScrolling(button);
		}
		update = 3;
	}

	private void LateUpdate()
	{
		_ = holding;
		if (holding == 0 && momentumDecayTime > 0f && velocity.sqrMagnitude > 1f)
		{
			Move(velocity * Time.unscaledDeltaTime);
			velocity /= 1f + Time.unscaledDeltaTime * (1f / momentumDecayTime);
		}
		if (update > 0)
		{
			Vector2 percentPosition = startPivot;
			if (FreeMoveSpace.x <= 0f)
			{
				percentPosition.x = backupPivot.x;
			}
			if (FreeMoveSpace.y <= 0f)
			{
				percentPosition.y = backupPivot.y;
			}
			PercentPosition = percentPosition;
			update--;
		}
	}

	private void OnEnable()
	{
		if (resetPositionAtEnable)
		{
			update = 3;
		}
	}

	public void ResetPosition()
	{
		update = 3;
	}

	public void RegisterScrolling(Button a, Func<bool> apply = null)
	{
		a.onDown += (Action)delegate
		{
			holding++;
		};
		a.onUp += (Action)delegate
		{
			holding--;
		};
		a.onHold += (Action<OnInputStayData>)delegate(OnInputStayData data)
		{
			if (apply == null || apply())
			{
				Drag(data);
			}
		};
		a.onScroll += (Action<float>)delegate(float f)
		{
			Move(new Vector2(f * 50f, f * 50f));
		};
	}

	public void RegisterScrolling(Screen_Game a)
	{
		a.onInputStart = (Action<OnInputStartData>)Delegate.Combine(a.onInputStart, (Action<OnInputStartData>)delegate
		{
			holding++;
		});
		a.onInputEnd = (Action<OnInputEndData>)Delegate.Combine(a.onInputEnd, (Action<OnInputEndData>)delegate
		{
			holding--;
		});
		a.onInputStay = (Action<OnInputStayData>)Delegate.Combine(a.onInputStay, new Action<OnInputStayData>(Drag));
	}

	public void Drag(OnInputStayData data)
	{
		Drag(data.delta.deltaPixel);
	}

	public void Drag(Vector3 deltaPixel)
	{
		DragDirect(base.transform.parent.InverseTransformVector(deltaPixel));
	}

	public void DragDirect(Vector2 delta)
	{
		velocity = delta / Time.unscaledDeltaTime;
		Move(delta);
	}

	public void Move(Vector2 delta)
	{
		Vector2 percentPosition = PercentPosition;
		delta = new Vector2(horizontal ? delta.x : 0f, vertical ? delta.y : 0f);
		GetRect().localPosition = (Vector2)GetRect().localPosition + delta;
		Vector2 percentPosition2 = PercentPosition;
		if (FreeMoveSpace.x <= 0f)
		{
			percentPosition2.x = backupPivot.x;
		}
		if (FreeMoveSpace.y <= 0f)
		{
			percentPosition2.y = backupPivot.y;
		}
		Vector2 rubberbandDistance = RubberbandDistance;
		PercentPosition = new Vector2(Mathf.Clamp01(percentPosition2.x), Mathf.Clamp01(percentPosition2.y));
	}

	private RectTransform GetRect()
	{
		return (RectTransform)base.transform;
	}

	private RectTransform GetParentRect()
	{
		return base.transform.parent.gameObject.Rect();
	}
}
