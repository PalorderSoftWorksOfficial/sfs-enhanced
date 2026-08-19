using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Input;
using SFS.IO;
using SFS.UI;
using SFS.Variables;
using UnityEngine;
using UnityEngine.UI;

namespace SFS;

public class VideoSettings : SettingsBase<VideoSettings.Data>
{
	[Serializable]
	public class Data
	{
		public bool freeOrientation = true;

		public int fps = 60;

		public float menuScale = 0.75f;

		public float menuOpacity = 1f;

		public bool cameraShake = true;

		public int orbitLinesCount = 3;

		public bool FXAA = true;
	}

	public static VideoSettings main;

	public ToggleButton autoRotate;

	public ToggleButton shakesToggle;

	public ToggleButton fxaaToggle;

	public Text fpsText;

	public FillSlider menuScaleSlider;

	public FillSlider menuOpacitySlider;

	public FillSlider conicSectionsCountSlider;

	public ContainerElement settingsMenuSizer;

	public Bool_Local enableFreeOrientation;

	public Float_Local menuScale;

	public Float_Local menuOpacity;

	public RectTransform fxaaHolder;

	public GameObject settingsManager;

	private readonly int[] refreshRateOptions = new int[7] { 30, 60, 75, 90, 100, 120, 144 };

	protected override string FileName => "VideoSettings";

	private void Awake()
	{
		main = this;
	}

	public float GetMenuScale()
	{
		return Mathf.Lerp(0.8f, 1.2f, menuScale.Value);
	}

	private void Start()
	{
		BindToggles();
		SubscribeToChanges();
		Load();
		CheckForFXAA();
		Security();
	}

	private void CheckForFXAA()
	{
		if (!Shader.Find("Hidden/FXAA3").isSupported)
		{
			settings.FXAA = false;
			fxaaHolder.gameObject.SetActive(value: false);
			LayoutRebuilder.ForceRebuildLayoutImmediate(fxaaHolder.parent.Rect());
			settingsMenuSizer.UpdateHierarchy();
		}
	}

	private void BindToggles()
	{
		autoRotate.Bind(ToggleScreenRotation, () => settings.freeOrientation);
		shakesToggle.Bind(ToggleShakes, () => settings.cameraShake);
		fxaaToggle.Bind(ToogleFXAA, () => settings.FXAA);
	}

	private void SubscribeToChanges()
	{
		enableFreeOrientation.OnChange += new Action(Apply);
		FillSlider fillSlider = menuScaleSlider;
		fillSlider.onSlide = (Action<float>)Delegate.Combine(fillSlider.onSlide, new Action<float>(OnMenuScaleChange));
		FillSlider fillSlider2 = menuOpacitySlider;
		fillSlider2.onSlide = (Action<float>)Delegate.Combine(fillSlider2.onSlide, new Action<float>(OnMenuOpacityChange));
		FillSlider fillSlider3 = conicSectionsCountSlider;
		fillSlider3.onSlide = (Action<float>)Delegate.Combine(fillSlider3.onSlide, new Action<float>(OnConicSectionsCountChange));
		menuScale.OnChange += new Action(settingsMenuSizer.ActAsRoot);
	}

	private void Security()
	{
	}

	private void Apply()
	{
		Screen.orientation = ((!settings.freeOrientation || !enableFreeOrientation.Value) ? ScreenOrientation.Portrait : ScreenOrientation.AutoRotation);
		Application.targetFrameRate = settings.fps;
	}

	private void UpdateUI(bool instantAnimation)
	{
		autoRotate.UpdateUI(instantAnimation);
		shakesToggle.UpdateUI(instantAnimation);
		fxaaToggle.UpdateUI(instantAnimation);
		fpsText.text = settings.fps.ToString();
	}

	private void OnMenuScaleChange(float value)
	{
		settings.menuScale = value;
		menuScale.Value = settings.menuScale;
		Save();
	}

	private void OnMenuOpacityChange(float value)
	{
		settings.menuOpacity = value;
		menuOpacity.Value = settings.menuOpacity;
		Save();
	}

	private void OnConicSectionsCountChange(float value)
	{
		settings.orbitLinesCount = Math.Clamp(Mathf.RoundToInt(value * 10f), 3, 10);
		conicSectionsCountSlider.SetFillAmount((float)settings.orbitLinesCount / 10f, invokeOnSlide: false);
		Save();
	}

	private void ToggleScreenRotation()
	{
		Data data = settings;
		data.freeOrientation = !data.freeOrientation;
		Apply();
		Save();
	}

	private void ToggleShakes()
	{
		Data data = settings;
		data.cameraShake = !data.cameraShake;
		Apply();
		Save();
	}

	private void ToogleFXAA()
	{
		Data data = settings;
		data.FXAA = !data.FXAA;
		Apply();
		Save();
	}

	public void OpenFPSMenu()
	{
		List<MenuElement> list = new List<MenuElement> { new SizeSyncerBuilder(out var carrier).HorizontalMode(SizeMode.MaxChildSize) };
		int[] array = refreshRateOptions;
		foreach (int num in array)
		{
			int copy = num;
			list.Add(ButtonBuilder.CreateButton(carrier, () => copy.ToString(), delegate
			{
				ScreenManager.main.CloseCurrent();
				settings.fps = copy;
				Apply();
				UpdateUI(instantAnimation: false);
				Save();
			}, CloseMode.None).MinSize(300f, 60f));
		}
		ScreenManager.main.OpenScreen(MenuGenerator.CreateMenu(CancelButton.None, CloseMode.None, delegate
		{
		}, delegate
		{
		}, list.ToArray()));
	}

	protected override void OnLoad()
	{
		if (!refreshRateOptions.Contains(settings.fps))
		{
			settings.fps = 30;
			Save();
		}
		if (settings.orbitLinesCount < 3)
		{
			settings.orbitLinesCount = 3;
			Save();
		}
		Apply();
		UpdateUI(instantAnimation: true);
		menuScaleSlider.SetFillAmount(settings.menuScale, invokeOnSlide: false);
		menuOpacitySlider.SetFillAmount(settings.menuOpacity, invokeOnSlide: false);
		conicSectionsCountSlider.SetFillAmount((float)settings.orbitLinesCount * 0.1f, invokeOnSlide: false);
		menuScale.Value = settings.menuScale;
		menuOpacity.Value = settings.menuOpacity;
	}
}
