using System;
using UnityEngine;

namespace SFS.Audio;

public class PlayingSoundEffect2D
{
	private Tuple<Sound, AudioSource>[] sources;

	private float volume;

	public PlayingSoundEffect2D(Tuple<Sound, AudioSource>[] sources, float volume)
	{
		this.sources = sources;
		this.volume = volume;
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
}
