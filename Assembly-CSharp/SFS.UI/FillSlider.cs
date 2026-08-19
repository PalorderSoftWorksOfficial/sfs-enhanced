using System;
using SFS.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class FillSlider : MonoBehaviour
{
	private float fillAmount;

	public Image slider;

	public Text percent;

	public string addAfterPercent = "%";

	public float valueRound = 0.05f;

	public Action<float> onSlide;

	public bool round;

	public float displayedRange_Start;

	public float displayedRange_End = 1f;

	public void SetFillAmount(float newFillAmount, bool invokeOnSlide)
	{
		newFillAmount = Mathf.Clamp01(newFillAmount);
		fillAmount = newFillAmount;
		float num = (round ? newFillAmount.Round(valueRound / (displayedRange_End - displayedRange_Start)) : newFillAmount);
		if (slider != null)
		{
			slider.fillAmount = num;
		}
		if (percent != null)
		{
			percent.text = (Mathf.Lerp(displayedRange_Start, displayedRange_End, num) * 100f).Round(valueRound * 100f) + addAfterPercent;
		}
		if (invokeOnSlide)
		{
			onSlide?.Invoke(num);
		}
	}

	private void Awake()
	{
		if (slider.fillMethod == Image.FillMethod.Horizontal || slider.fillMethod == Image.FillMethod.Vertical)
		{
			GetComponent<Button>().onHold += new Action<OnInputStayData>(TouchStay);
		}
	}

	private void TouchStay(OnInputStayData data)
	{
		SetFillAmount(fillAmount + GetDelta(data.delta.deltaPixel), invokeOnSlide: true);
	}

	private float GetDelta(Vector2 deltaPixel)
	{
		deltaPixel = base.transform.InverseTransformVector(deltaPixel);
		return slider.fillMethod switch
		{
			Image.FillMethod.Horizontal => deltaPixel.x / slider.rectTransform.rect.width, 
			Image.FillMethod.Vertical => deltaPixel.y / slider.rectTransform.rect.height, 
			_ => throw new Exception(), 
		};
	}
}
