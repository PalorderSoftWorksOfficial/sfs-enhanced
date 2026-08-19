using System;
using Beebyte.Obfuscator;
using SFS.IO;
using SFS.UI;
using SFS.Variables;
using UnityEngine.UI;

namespace SFS.Audio;

public class AudioSettings : SettingsBase<AudioSettings.Data>
{
	[Serializable]
	[Skip]
	public class Data
	{
		public float soundVolume = 1f;

		public float musicVolume = 1f;
	}

	public static AudioSettings main;

	public FillSlider soundSlider;

	public FillSlider musicSlider;

	public Slider soundSliderPC;

	public Slider musicSliderPC;

	public Float_Local soundVolume;

	public Float_Local musicVolume;

	protected override string FileName => "AudioSettings";

	private void Awake()
	{
		main = this;
	}

	private void Start()
	{
		Load();
		soundSliderPC.onValueChanged.AddListener(OnSoundChange);
		musicSliderPC.onValueChanged.AddListener(OnMusicChange);
	}

	protected override void OnLoad()
	{
		soundSliderPC.value = settings.soundVolume;
		musicSliderPC.value = settings.musicVolume;
		soundVolume.Value = settings.soundVolume;
		musicVolume.Value = settings.musicVolume;
	}

	private void OnSoundChange(float value)
	{
		settings.soundVolume = value;
		soundVolume.Value = value;
		Save();
	}

	private void OnMusicChange(float value)
	{
		settings.musicVolume = value;
		musicVolume.Value = value;
		Save();
	}
}
