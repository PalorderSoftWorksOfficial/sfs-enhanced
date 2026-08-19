using System;
using System.Linq;
using UnityEngine;

namespace SFS.Audio;

public class PlayingSoundEffect3D
{
	private Tuple<Sound, AudioSource>[] sources;

	private float volume;

	public bool IsPlaying => sources.Any((Tuple<Sound, AudioSource> a) => a.Item2.isPlaying);

	public PlayingSoundEffect3D(Tuple<Sound, AudioSource>[] sources, float volume)
	{
		this.sources = sources;
		this.volume = volume;
	}

	public void Play()
	{
		Stop();
		Tuple<Sound, AudioSource>[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			var (sound2, audioSource2) = array[i];
			if (sound2.delay > 0f)
			{
				audioSource2.PlayDelayed(sound2.delay);
			}
			else
			{
				audioSource2.Play();
			}
		}
	}

	public void Stop()
	{
		Tuple<Sound, AudioSource>[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Item2.Stop();
		}
	}

	public void SetVolume(float volume)
	{
		this.volume = volume;
		ApplyVolume();
	}

	public void ApplyVolume()
	{
		Tuple<Sound, AudioSource>[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			var (sound2, audioSource2) = array[i];
			if (audioSource2 != null)
			{
				audioSource2.volume = sound2.volume * volume * (float)AudioSettings.main.soundVolume;
			}
		}
	}

	public void SetPitch(float pitch)
	{
		Tuple<Sound, AudioSource>[] array = sources;
		for (int i = 0; i < array.Length; i++)
		{
			var (sound2, audioSource2) = array[i];
			audioSource2.pitch = sound2.pitch * pitch;
		}
	}
}
