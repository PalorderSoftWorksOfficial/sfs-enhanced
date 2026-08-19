using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Audio;
using SFS.Input;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

[ExecuteAlways]
public class Button : MonoBehaviour, I_Key, I_Touchable, I_Raycastable
{
	public bool advancedBorder;

	public float border;

	public float top;

	public float bottom;

	public float left;

	public float right;

	public bool skipRaycast;

	public int layoutPriority;

	public HoldUnityEvent holdEvent;

	public ClickUnityEvent clickEvent;

	public OptionalDelegate<OnInputStartData> onDown = new OptionalDelegate<OnInputStartData>();

	public OptionalDelegate<OnInputStayData> onHold = new OptionalDelegate<OnInputStayData>();

	public OptionalDelegate<OnInputEndData> onUp = new OptionalDelegate<OnInputEndData>();

	public OptionalDelegate<OnInputEndData> onClick = new OptionalDelegate<OnInputEndData>();

	public OptionalDelegate<OnInputEndData> onClickDisabled = new OptionalDelegate<OnInputEndData>();

	public OptionalDelegate<OnInputEndData> onRightClick = new OptionalDelegate<OnInputEndData>();

	public OptionalDelegate<OnTouchLongClickData> onLongClick = new OptionalDelegate<OnTouchLongClickData>();

	public OptionalDelegate<float> onScroll = new OptionalDelegate<float>();

	public OptionalDelegate<ZoomData> onZoom = new OptionalDelegate<ZoomData>();

	public bool noClickSound;

	[HideInInspector]
	public Bool_Local onKeyDown;

	[HideInInspector]
	public Bool_Local onKey;

	[HideInInspector]
	public Bool_Local onKeyUp;

	protected bool buttonEnabled = true;

	private Vector2 initialIconScale;

	private InputMask2D[] inputMasks;

	bool I_Raycastable.SkipRaycast => skipRaycast;

	private InputMask2D[] InputMasks => inputMasks ?? (inputMasks = GetComponentsInParent<InputMask2D>(includeInactive: true));

	protected virtual void Start()
	{
		onKey = new Bool_Local();
		onKeyDown = new Bool_Local();
		onKeyUp = new Bool_Local();
		initialIconScale = GetIconScale();
		onKey.OnChange += new Action(OnKeyChange);
		onClick += (Action<OnInputEndData>)delegate(OnInputEndData data)
		{
			clickEvent.Invoke(data);
		};
		onHold += (Action<OnInputStayData>)delegate(OnInputStayData data)
		{
			holdEvent.Invoke(data);
		};
	}

	private void OnKeyChange()
	{
		ScaleTextToFit componentInChildren = GetComponentInChildren<ScaleTextToFit>();
		Vector2 vector = ((componentInChildren != null) ? componentInChildren.latestScale : initialIconScale);
		base.transform.Find("Glow")?.gameObject.SetActive(onKey.Value);
		SetIconScale(vector * (onKey ? 1.08f : 1f));
	}

	protected virtual void OnEnable()
	{
		TouchElements.AddElement(this);
	}

	protected virtual void OnDisable()
	{
		TouchElements.RemoveElement(this);
	}

	public virtual void SetEnabled(bool enabled)
	{
		if (this == null)
		{
			return;
		}
		buttonEnabled = enabled;
		Text componentInChildren = GetComponentInChildren<Text>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.color = (enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f));
		}
		Transform transform = base.transform.Find("Icon") ?? base.transform.Find("Button Icon");
		if (transform != null)
		{
			Image[] componentsInChildren = transform.GetComponentsInChildren<Image>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = (enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f));
			}
		}
	}

	private Vector3 GetIconScale()
	{
		Transform transform = base.transform.Find("Icon") ?? base.transform.Find("Text");
		if (transform != null)
		{
			return transform.localScale;
		}
		return Vector2.one;
	}

	private void SetIconScale(Vector3 newIconScale)
	{
		Transform transform = base.transform.Find("Icon") ?? base.transform.Find("Text");
		if (transform != null)
		{
			transform.localScale = newIconScale;
		}
	}

	bool I_Key.IsKeyDown()
	{
		return onKeyDown;
	}

	bool I_Key.IsKeyStay()
	{
		return onKey;
	}

	bool I_Key.IsKeyUp()
	{
		return onKeyUp;
	}

	RectTransform I_Raycastable.GetRect()
	{
		return GetComponent<RectTransform>();
	}

	List<int> I_Touchable.GetPriority()
	{
		return TouchElements.GetPriority(base.transform);
	}

	bool I_Touchable.PointcastElement(TouchPosition position)
	{
		if (InputMasks.Where((InputMask2D x) => x.isActiveAndEnabled).All((InputMask2D x) => x.Pointcast(position.pixel)))
		{
			return TouchElements.PointcastElement(position, base.transform, advancedBorder, border, top, bottom, left, right);
		}
		return false;
	}

	void I_Touchable.OnInputStart(OnInputStartData data)
	{
		if (buttonEnabled && (data.inputType == InputType.MouseLeft || data.inputType == InputType.Touch))
		{
			onKeyDown.Value = true;
			Base.inputManager.onReset += delegate
			{
				onKeyDown.Value = false;
			};
			onKey.Value = true;
			onDown.Invoke(data);
		}
	}

	void I_Touchable.OnInputStay(OnInputStayData data)
	{
		if (buttonEnabled && (data.inputType == InputType.MouseLeft || data.inputType == InputType.Touch))
		{
			onHold.Invoke(data);
		}
	}

	void I_Touchable.OnInputEnd(OnInputEndData data)
	{
		if (buttonEnabled)
		{
			onKeyUp.Value = true;
			Base.inputManager.onReset += delegate
			{
				onKeyUp.Value = false;
			};
			onKey.Value = false;
			onUp.Invoke(data);
		}
		if (data.LeftClick)
		{
			if ((onClick.CallCount > 1 || clickEvent.GetPersistentEventCount() > 0) && buttonEnabled && !noClickSound)
			{
				SoundPlayer.main.clickSound.Play();
			}
			if (buttonEnabled)
			{
				onClick.Invoke(data);
			}
			else
			{
				onClickDisabled.Invoke(data);
			}
		}
		if (data.click && data.inputType == InputType.MouseRight)
		{
			if (onRightClick.CallCount > 1 && buttonEnabled && !noClickSound)
			{
				SoundPlayer.main.clickSound.Play();
			}
			if (buttonEnabled)
			{
				onRightClick.Invoke(data);
			}
		}
	}

	void I_Touchable.OnTouchLongClick(OnTouchLongClickData data)
	{
		onLongClick.Invoke(data);
	}

	void I_Touchable.OnNotStationary(OnNotStationary data)
	{
	}

	void I_Touchable.OnDrag(DragData data)
	{
	}

	void I_Touchable.OnZoom(ZoomData data)
	{
		onZoom?.Invoke(data);
	}

	public virtual void OnMouseEnter()
	{
	}

	public virtual void OnMouseExit()
	{
	}
}
