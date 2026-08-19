using System;
using SFS.Audio;
using SFS.Variables;
using UnityEngine;

namespace SFS.Parts.Modules;

public class AudioModule : MonoBehaviour
{
	public SoundEffect soundEffect;

	public Composed_Float volume = new Composed_Float("1");

	public Composed_Float pitch = new Composed_Float("1");

	public bool playOnStart;

	public bool loop;

	private bool init;

	private PlayingSoundEffect3D playingSoundEffect;

	private void Start()
	{
		volume.OnChange += new Action(OnVolumeChange);
		pitch.OnChange += new Action(OnPitchChange);
		init = true;
		if (playOnStart)
		{
			Play();
		}
	}

	private void OnVolumeChange()
	{
		if (init)
		{
			if (volume.Value > 0f)
			{
				Play();
			}
			else
			{
				Stop();
			}
			playingSoundEffect?.SetVolume(volume.Value);
		}
	}

	private void OnPitchChange()
	{
		playingSoundEffect?.SetPitch(pitch.Value);
	}

	private void Play()
	{
		if (playingSoundEffect != null)
		{
			if (!playingSoundEffect.IsPlaying)
			{
				playingSoundEffect.Play();
			}
		}
		else
		{
			playingSoundEffect = soundEffect.Play3D(base.transform, volume.Value, loop);
		}
	}

	private void Stop()
	{
		playingSoundEffect?.Stop();
	}
}
