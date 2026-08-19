using System;
using System.Collections.Generic;
using System.Linq;
using SFS.IO;
using SFS.Translations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SFS;

public class VideoSettingsPC : SettingsBase<VideoSettingsPC.Data>
{
	[Serializable]
	public class Data
	{
		public int windowMode;

		public Vector2 currentResolution = new Vector2(0f, 0f);

		public float uiScale = 1f;

		public float uiOpacity = 1f;

		public int verticalSync = 1;

		public int fps = -1;

		public bool cameraShake = true;

		public int orbitLinesCount = 3;

		public bool FXAA = true;
	}

	public static VideoSettingsPC main;

	public TMP_Dropdown videoResolutionDropdown;

	public TMP_Dropdown windowModeDropdown;

	public Slider uiScaleSlider;

	public Slider uiOpacitySlider;

	public Slider conicSectionsCountSlider;

	public TMP_Dropdown verticalSyncDropdown;

	public TMP_Dropdown fpsDropdown;

	public TMP_Dropdown shakesDropdown;

	public TMP_Dropdown antiAliasingDropdown;

	public UnityEvent onScaleUIChanged;

	public UnityEvent onOpacityChanged;

	private List<Resolution> resolutionList = new List<Resolution>();

	private List<string> fpsOptions;

	private List<string> windowModeOptions;

	private bool isInit;

	protected override string FileName => "VideoSettings";

	private void Awake()
	{
		main = this;
	}

	protected override void OnLoad()
	{
		Init();
		Apply();
		UpdateUI();
	}

	private void Start()
	{
		Load();
		Init();
		Apply();
		UpdateUI();
		windowModeDropdown.onValueChanged.AddListener(WindowModeDropdownChanged);
		videoResolutionDropdown.onValueChanged.AddListener(VideoResolutionDropdownChanged);
		uiScaleSlider.onValueChanged.AddListener(UIScaleChanged);
		uiOpacitySlider.onValueChanged.AddListener(UIOpacityChanged);
		verticalSyncDropdown.onValueChanged.AddListener(VerticalSyncDropdownChanged);
		fpsDropdown.onValueChanged.AddListener(FpsDropdownChanged);
		shakesDropdown.onValueChanged.AddListener(ShakesDropdownChanged);
		conicSectionsCountSlider.onValueChanged.AddListener(OnConicSectionsCountChange);
		antiAliasingDropdown.onValueChanged.AddListener(AntiAliasingDropdownChanged);
	}

	private void Apply()
	{
		onScaleUIChanged.Invoke();
		onOpacityChanged.Invoke();
		if (settings.currentResolution.x == 0f && settings.currentResolution.y == 0f)
		{
			settings.currentResolution = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
		}
		QualitySettings.vSyncCount = settings.verticalSync;
		Application.targetFrameRate = settings.fps;
		if (Screen.resolutions.Any((Resolution p) => (float)p.width == settings.currentResolution.x && (float)p.height == settings.currentResolution.y))
		{
			Screen.SetResolution((int)settings.currentResolution.x, (int)settings.currentResolution.y, (FullScreenMode)settings.windowMode);
		}
		else
		{
			Screen.SetResolution(Display.main.systemWidth, Display.main.systemWidth, FullScreenMode.ExclusiveFullScreen);
		}
	}

	private void UpdateUI()
	{
		Init();
		Resolution item = resolutionList.FirstOrDefault((Resolution p) => p.width == Screen.currentResolution.width && p.height == Screen.currentResolution.height);
		videoResolutionDropdown.value = resolutionList.IndexOf(item, 0);
		windowModeDropdown.value = settings.windowMode;
		uiScaleSlider.value = settings.uiScale;
		verticalSyncDropdown.value = ((settings.verticalSync != 1) ? 1 : 0);
		fpsDropdown.value = ((settings.fps != -1) ? fpsOptions.IndexOf(settings.fps.ToString()) : 0);
		shakesDropdown.value = ((!settings.cameraShake) ? 1 : 0);
		conicSectionsCountSlider.value = settings.orbitLinesCount;
		antiAliasingDropdown.value = (settings.FXAA ? 1 : 0);
	}

	private void Init()
	{
		if (isInit)
		{
			return;
		}
		windowModeDropdown.ClearOptions();
		windowModeOptions = new List<string>
		{
			Loc.main.Fullscreen_Exclusive,
			Loc.main.Fullscreen_Borderless,
			Loc.main.Fullscreen_Windowed
		};
		windowModeDropdown.AddOptions(windowModeOptions);
		videoResolutionDropdown.ClearOptions();
		List<string> list = new List<string>();
		Resolution[] resolutions = Screen.resolutions;
		resolutionList = new List<Resolution>();
		Resolution[] array = resolutions;
		for (int i = 0; i < array.Length; i++)
		{
			Resolution res = array[i];
			if (resolutionList.Count((Resolution p) => p.width == res.width && p.height == res.height) != 1)
			{
				list.Add(res.width + "x" + res.height);
				resolutionList.Add(res);
			}
		}
		videoResolutionDropdown.AddOptions(list);
		verticalSyncDropdown.ClearOptions();
		List<string> list2 = new List<string>();
		list2.Add(Loc.main.State_On);
		list2.Add(Loc.main.State_Off);
		verticalSyncDropdown.AddOptions(list2);
		fpsDropdown.ClearOptions();
		fpsOptions = new List<string>();
		fpsOptions.Add(Loc.main.Fps_Unlimited);
		fpsOptions.Add("30");
		fpsOptions.Add("60");
		fpsOptions.Add("100");
		fpsOptions.Add("120");
		fpsOptions.Add("144");
		fpsOptions.Add("240");
		fpsDropdown.AddOptions(fpsOptions);
		shakesDropdown.ClearOptions();
		shakesDropdown.AddOptions(new List<string>
		{
			Loc.main.State_On,
			Loc.main.State_Off
		});
		antiAliasingDropdown.ClearOptions();
		antiAliasingDropdown.AddOptions(new List<string> { "Off", "FXAA" });
		if (settings.orbitLinesCount < 3)
		{
			settings.orbitLinesCount = 3;
		}
		isInit = true;
	}

	private void UIScaleChanged(float newValue)
	{
		settings.uiScale = newValue;
		onScaleUIChanged.Invoke();
		Save();
	}

	private void UIOpacityChanged(float newValue)
	{
		settings.uiOpacity = newValue;
		onOpacityChanged.Invoke();
		Save();
	}

	private void OnConicSectionsCountChange(float newValue)
	{
		settings.orbitLinesCount = (int)newValue;
		Save();
	}

	private void FpsDropdownChanged(int index)
	{
		settings.fps = ((index == 0) ? (-1) : Convert.ToInt32(fpsOptions[index]));
		Apply();
		Save();
	}

	private void ShakesDropdownChanged(int index)
	{
		settings.cameraShake = index == 0;
		Apply();
		Save();
	}

	private void VerticalSyncDropdownChanged(int index)
	{
		settings.verticalSync = ((index == 0) ? 1 : 0);
		Apply();
		Save();
	}

	private void VideoResolutionDropdownChanged(int index)
	{
		settings.currentResolution = new Vector2(resolutionList[index].width, resolutionList[index].height);
		Apply();
		Save();
	}

	private void WindowModeDropdownChanged(int index)
	{
		switch (index)
		{
		case 0:
			settings.windowMode = 0;
			break;
		case 1:
			settings.windowMode = 2;
			break;
		case 2:
			settings.windowMode = 3;
			break;
		}
		Apply();
		Save();
	}

	private void AntiAliasingDropdownChanged(int index)
	{
		settings.FXAA = index == 1;
		Save();
	}
}
