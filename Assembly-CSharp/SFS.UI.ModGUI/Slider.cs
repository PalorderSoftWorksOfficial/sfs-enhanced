using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SFS.UI.ModGUI;

public class Slider : GUIElement
{
	private UnityEngine.UI.Slider slider;

	private TextAdapter valueText;

	public UnityAction<float> OnSliderChanged
	{
		set
		{
			slider.onValueChanged.AddListener(value);
		}
	}

	public Func<float, string> GetValueWithUnit
	{
		set
		{
			OnSliderChanged = delegate(float f)
			{
				valueText.Text = value(f);
			};
		}
	}

	public UnityEngine.UI.Slider.Direction SliderType
	{
		get
		{
			return slider.direction;
		}
		set
		{
			slider.direction = value;
		}
	}

	public (float min, float max) MinMaxValue
	{
		get
		{
			return (min: slider.minValue, max: slider.maxValue);
		}
		set
		{
			slider.minValue = value.min;
			slider.maxValue = value.max;
		}
	}

	public bool WholeNumbersOnly
	{
		get
		{
			return slider.wholeNumbers;
		}
		set
		{
			slider.wholeNumbers = value;
		}
	}

	public float Value
	{
		get
		{
			return slider.value;
		}
		set
		{
			slider.value = value;
		}
	}

	public override void Init(GameObject self, Transform parent)
	{
		gameObject = self;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		rectTransform = gameObject.Rect();
		slider = gameObject.GetComponent<UnityEngine.UI.Slider>();
		valueText = rectTransform.Find("MediumText").GetComponent<TextAdapter>();
		valueText.Text = string.Empty;
	}
}
