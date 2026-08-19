using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SFS.UI;

public class TextAdapter : MonoBehaviour
{
	private Text UnityText;

	private TextMeshProUGUI TMProText;

	private bool isInit;

	public string Text
	{
		get
		{
			Init();
			if (UnityText != null)
			{
				return UnityText.text;
			}
			if (TMProText != null)
			{
				return TMProText.text;
			}
			return null;
		}
		set
		{
			Init();
			if (UnityText != null)
			{
				UnityText.text = value;
			}
			if (TMProText != null)
			{
				TMProText.text = value;
			}
		}
	}

	public Color Color
	{
		get
		{
			Init();
			if (UnityText != null)
			{
				return UnityText.color;
			}
			if (TMProText != null)
			{
				return TMProText.color;
			}
			return Color.clear;
		}
		set
		{
			Init();
			if (UnityText != null)
			{
				UnityText.color = value;
			}
			if (TMProText != null)
			{
				TMProText.color = value;
			}
		}
	}

	private void Awake()
	{
		Init();
		if (TMProText != null)
		{
			TMProText.ForceMeshUpdate();
		}
	}

	private void Init()
	{
		if (!isInit)
		{
			UnityText = GetComponent<Text>();
			TMProText = GetComponent<TextMeshProUGUI>();
			isInit = true;
		}
	}
}
