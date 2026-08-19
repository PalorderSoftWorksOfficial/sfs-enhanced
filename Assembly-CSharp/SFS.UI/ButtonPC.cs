using System;
using SFS.Tween;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SFS.UI;

public class ButtonPC : Button
{
	[FormerlySerializedAs("buttonBackOver")]
	public Image ButtonBackOver;

	[FormerlySerializedAs("buttonText")]
	public TextMeshProUGUI ButtonText;

	[FormerlySerializedAs("buttonIcon")]
	public Image ButtonIcon;

	[FormerlySerializedAs("buttonOverIcon")]
	public Image ButtonOverIcon;

	[FormerlySerializedAs("color")]
	public Color Color;

	[FormerlySerializedAs("overColor")]
	public Color OverColor;

	[NonSerialized]
	public bool useLongClick;

	private bool selected;

	private bool over;

	private bool isDestroy;

	public void SetSelected(bool selected)
	{
		this.selected = selected;
		SetState(over, 0f);
	}

	public override void SetEnabled(bool enabled)
	{
		if (!(this == null))
		{
			base.SetEnabled(enabled);
			SetState(over, 0f);
		}
	}

	private void Awake()
	{
		SetState(over: false, 0f);
	}

	public override void OnMouseEnter()
	{
		SetState(over: true);
	}

	public override void OnMouseExit()
	{
		SetState(over: false);
	}

	private void SetState(bool over, float fadeDuration = 0.1f)
	{
		if (!isDestroy)
		{
			this.over = over;
			bool flag = (over && buttonEnabled) || selected;
			Color target = (flag ? OverColor : Color);
			if (!buttonEnabled)
			{
				target.a *= 0.4f;
			}
			if (ButtonBackOver != null)
			{
				ButtonBackOver.TweenFade(flag ? 1 : 0, fadeDuration, setValueToTargetOnKill: true);
			}
			if (ButtonText != null)
			{
				ButtonText.TweenColor(target, fadeDuration, setValueToTargetOnKill: true);
			}
			if (ButtonOverIcon != null && ButtonIcon != null)
			{
				ButtonOverIcon.TweenFade(flag ? 1 : 0, fadeDuration, setValueToTargetOnKill: true);
				ButtonIcon.TweenFade((!flag) ? 1 : 0, fadeDuration, setValueToTargetOnKill: true);
			}
			else if (ButtonIcon != null)
			{
				ButtonIcon.TweenColor(target, fadeDuration, setValueToTargetOnKill: true);
			}
		}
	}

	private void OnDestroy()
	{
		isDestroy = true;
		KillTween();
	}

	protected override void OnDisable()
	{
		KillTween();
		base.OnDisable();
	}

	private void KillTween()
	{
		if (ButtonBackOver != null)
		{
			ButtonBackOver.TweenKill();
		}
		if (ButtonIcon != null)
		{
			ButtonIcon.TweenKill();
		}
		if (ButtonText != null)
		{
			ButtonText.TweenKill();
		}
		if (ButtonOverIcon != null)
		{
			ButtonOverIcon.TweenKill();
		}
	}
}
