using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class SliderText : MonoBehaviour
{
	public Slider slider;

	public TextMeshProUGUI sliderTextField;

	public string addAfterValue = "%";

	public float valueMultiplier = 100f;

	private void Start()
	{
		slider.onValueChanged.AddListener(valueChanged);
		valueChanged(slider.value);
	}

	private void valueChanged(float a)
	{
		sliderTextField.text = Math.Round(a * valueMultiplier) + addAfterValue;
	}
}
